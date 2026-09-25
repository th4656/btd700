//! Qualcomm GAIA Protocol client for Sennheiser Bluetooth Headphones (HDB 630, Momentum 4, etc.).
//! Communicates over native Linux Bluetooth RFCOMM sockets (`AF_BLUETOOTH`).

use std::fs;
use std::io;
use std::os::unix::io::RawFd;
use std::path::PathBuf;
use std::process::Command;
use std::time::{Duration, Instant};
use serde::{Deserialize, Serialize};

use crate::constants::*;

pub const BTPROTO_RFCOMM: libc::c_int = 3;

#[repr(C)]
#[derive(Debug, Copy, Clone)]
pub struct SockaddrRc {
    pub rc_family: libc::sa_family_t,
    pub rc_bdaddr: [u8; 6],
    pub rc_channel: u8,
    pub _padding: u8,
}

pub fn parse_mac_to_bdaddr(mac: &str) -> Option<[u8; 6]> {
    let parts: Vec<&str> = mac.split(':').collect();
    if parts.len() != 6 {
        return None;
    }
    let mut bytes = [0u8; 6];
    for (i, part) in parts.iter().enumerate() {
        bytes[5 - i] = u8::from_str_radix(part, 16).ok()?;
    }
    Some(bytes)
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PairedDevice {
    pub mac: String,
    pub name: String,
    pub recommended: bool,
}

#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct HeadsetConfig {
    pub headset_mac: Option<String>,
    pub cached_channel: Option<u8>,
}

pub fn get_config_path() -> PathBuf {
    let home = std::env::var("HOME").unwrap_or_else(|_| ".".to_string());
    PathBuf::from(home).join(".config").join("btd700").join("config.json")
}

pub fn load_config() -> HeadsetConfig {
    let path = get_config_path();
    if let Ok(data) = fs::read_to_string(&path) {
        if let Ok(cfg) = serde_json::from_str(&data) {
            return cfg;
        }
    }
    HeadsetConfig::default()
}

pub fn save_config(cfg: &HeadsetConfig) {
    let path = get_config_path();
    if let Some(parent) = path.parent() {
        let _ = fs::create_dir_all(parent);
    }
    if let Ok(json) = serde_json::to_string_pretty(cfg) {
        let _ = fs::write(path, json);
    }
}

pub fn find_paired_headsets() -> Vec<PairedDevice> {
    let mut results = Vec::new();
    if let Ok(output) = Command::new("bluetoothctl").args(["--timeout", "2", "devices"]).output() {
        let stdout = String::from_utf8_lossy(&output.stdout);
        for line in stdout.lines() {
            let parts: Vec<&str> = line.split_whitespace().collect();
            if parts.len() >= 3 && parts[0] == "Device" {
                let mac = parts[1].to_uppercase();
                let name = parts[2..].join(" ");
                let lower = name.to_lowercase();
                let rec = lower.contains("hdb")
                    || lower.contains("630")
                    || lower.contains("sennheiser")
                    || lower.contains("momentum")
                    || lower.contains("accentum")
                    || lower.contains("hd 450")
                    || lower.contains("hd 350");
                results.push(PairedDevice {
                    mac,
                    name,
                    recommended: rec,
                });
            }
        }
    }
    results.sort_by_key(|d| !d.recommended);
    results
}

pub fn get_default_mac() -> Option<String> {
    let cfg = load_config();
    if let Some(mac) = cfg.headset_mac {
        if !mac.is_empty() {
            return Some(mac.to_uppercase());
        }
    }
    let devs = find_paired_headsets();
    for d in &devs {
        if d.recommended {
            return Some(d.mac.clone());
        }
    }
    devs.first().map(|d| d.mac.clone())
}

#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct HeadsetState {
    pub mac: String,
    pub connected: bool,
    pub anc_enabled: bool,
    pub anc_strength: i32,
    pub transparency: i32,
    pub adaptive_anc: bool,
    pub anti_wind: String,
    pub bass_boost: bool,
    pub channel: Option<u8>,
}

pub struct GaiaProtocol;

impl GaiaProtocol {
    pub fn build(command: u16, payload: &[u8]) -> Vec<u8> {
        let mut pkt = Vec::with_capacity(8 + payload.len());
        pkt.extend_from_slice(&GAIA_MAGIC);
        pkt.extend_from_slice(&(payload.len() as u16).to_be_bytes());
        pkt.extend_from_slice(&VENDOR_SENNHEISER.to_be_bytes());
        pkt.extend_from_slice(&command.to_be_bytes());
        pkt.extend_from_slice(payload);
        pkt
    }

    pub fn parse_many(buffer: &[u8]) -> (Vec<(u16, u16, Vec<u8>)>, usize) {
        let mut packets = Vec::new();
        let mut offset = 0;

        while buffer.len().saturating_sub(offset) >= 8 {
            if buffer[offset..offset + 2] != GAIA_MAGIC {
                if let Some(pos) = buffer[offset + 1..]
                    .windows(2)
                    .position(|w| w == GAIA_MAGIC)
                {
                    offset += 1 + pos;
                    continue;
                } else {
                    return (packets, buffer.len());
                }
            }

            let payload_len = u16::from_be_bytes([buffer[offset + 2], buffer[offset + 3]]) as usize;
            let total = 8 + payload_len;
            if buffer.len() - offset < total {
                break;
            }

            let vendor = u16::from_be_bytes([buffer[offset + 4], buffer[offset + 5]]);
            let command = u16::from_be_bytes([buffer[offset + 6], buffer[offset + 7]]);
            let payload = buffer[offset + 8..offset + total].to_vec();
            packets.push((vendor, command, payload));
            offset += total;
        }

        (packets, offset)
    }
}

pub struct SennheiserHeadset {
    pub mac: String,
    pub channel: Option<u8>,
    sock: Option<RawFd>,
    buffer: Vec<u8>,
}

impl SennheiserHeadset {
    pub fn new(mac: Option<String>) -> Self {
        let target_mac = mac.or_else(get_default_mac).unwrap_or_default().to_uppercase();
        Self {
            mac: target_mac,
            channel: None,
            sock: None,
            buffer: Vec::new(),
        }
    }

    pub fn is_connected(&self) -> bool {
        self.sock.is_some()
    }

    pub fn close(&mut self) {
        if let Some(fd) = self.sock.take() {
            unsafe { libc::close(fd) };
        }
        self.channel = None;
        self.buffer.clear();
    }

    fn connect_channel(&mut self, channel: u8, timeout: Duration) -> io::Result<()> {
        self.close();
        let bdaddr = parse_mac_to_bdaddr(&self.mac)
            .ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "Invalid MAC"))?;

        let fd = unsafe {
            libc::socket(
                libc::AF_BLUETOOTH,
                libc::SOCK_STREAM,
                BTPROTO_RFCOMM,
            )
        };
        if fd < 0 {
            return Err(io::Error::last_os_error());
        }

        // Set nonblocking to handle connect timeout
        unsafe {
            let flags = libc::fcntl(fd, libc::F_GETFL, 0);
            libc::fcntl(fd, libc::F_SETFL, flags | libc::O_NONBLOCK);
        }

        let addr = SockaddrRc {
            rc_family: libc::AF_BLUETOOTH as libc::sa_family_t,
            rc_bdaddr: bdaddr,
            rc_channel: channel,
            _padding: 0,
        };

        let ret = unsafe {
            libc::connect(
                fd,
                &addr as *const _ as *const libc::sockaddr,
                std::mem::size_of::<SockaddrRc>() as libc::socklen_t,
            )
        };

        if ret < 0 {
            let err = io::Error::last_os_error();
            if err.raw_os_error() != Some(libc::EINPROGRESS) {
                unsafe { libc::close(fd) };
                return Err(err);
            }

            // Wait with poll
            let mut pfd = libc::pollfd {
                fd,
                events: libc::POLLOUT,
                revents: 0,
            };
            let poll_ret = unsafe { libc::poll(&mut pfd, 1, timeout.as_millis() as libc::c_int) };
            if poll_ret <= 0 {
                unsafe { libc::close(fd) };
                return Err(io::Error::new(io::ErrorKind::TimedOut, "Connect timeout"));
            }

            let mut sock_err: libc::c_int = 0;
            let mut len = std::mem::size_of::<libc::c_int>() as libc::socklen_t;
            unsafe {
                libc::getsockopt(
                    fd,
                    libc::SOL_SOCKET,
                    libc::SO_ERROR,
                    &mut sock_err as *mut _ as *mut libc::c_void,
                    &mut len,
                );
            }
            if sock_err != 0 {
                unsafe { libc::close(fd) };
                return Err(io::Error::from_raw_os_error(sock_err));
            }
        }

        self.sock = Some(fd);
        self.channel = Some(channel);
        Ok(())
    }

    fn exchange(&mut self, command: u16, payload: &[u8], timeout: Duration) -> io::Result<(u16, u16, Vec<u8>)> {
        let fd = self.sock.ok_or_else(|| io::Error::new(io::ErrorKind::NotConnected, "Not connected"))?;
        let packet = GaiaProtocol::build(command, payload);

        let n = unsafe {
            libc::send(
                fd,
                packet.as_ptr() as *const libc::c_void,
                packet.len(),
                0,
            )
        };
        if n < 0 {
            return Err(io::Error::last_os_error());
        }

        let expected_resp = command | 0x0100;
        let expected_err = command | 0x0180;
        let start = Instant::now();

        while start.elapsed() < timeout {
            let (packets, consumed) = GaiaProtocol::parse_many(&self.buffer);
            self.buffer.drain(..consumed);

            for (v, c, p) in packets {
                if c == expected_resp || c == expected_err {
                    return Ok((v, c, p));
                }
            }

            let rem = timeout.saturating_sub(start.elapsed()).as_millis().max(50) as libc::c_int;
            let mut pfd = libc::pollfd {
                fd,
                events: libc::POLLIN,
                revents: 0,
            };
            let poll_ret = unsafe { libc::poll(&mut pfd, 1, rem) };
            if poll_ret > 0 && (pfd.revents & libc::POLLIN) != 0 {
                let mut buf = [0u8; 1024];
                let rd = unsafe {
                    libc::recv(
                        fd,
                        buf.as_mut_ptr() as *mut libc::c_void,
                        buf.len(),
                        0,
                    )
                };
                if rd <= 0 {
                    return Err(io::Error::new(io::ErrorKind::ConnectionReset, "RFCOMM connection closed"));
                }
                self.buffer.extend_from_slice(&buf[..rd as usize]);
            }
        }

        Err(io::Error::new(io::ErrorKind::TimedOut, "Command timed out"))
    }

    pub fn connect(&mut self, timeout: Duration) -> io::Result<u8> {
        if self.mac.is_empty() {
            return Err(io::Error::new(
                io::ErrorKind::NotFound,
                "No Sennheiser headset MAC configured or found in paired devices",
            ));
        }

        let mut cfg = load_config();
        let mut channels = COMMON_RFCOMM_CHANNELS.to_vec();
        if let Some(cached) = cfg.cached_channel {
            if let Some(pos) = channels.iter().position(|&c| c == cached) {
                channels.remove(pos);
                channels.insert(0, cached);
            }
        }

        for &ch in &channels {
            if self.connect_channel(ch, timeout).is_ok() {
                if let Ok((v, c, _)) = self.exchange(GAIA_CMD_GET_ANC_STATUS, &[], Duration::from_millis(1500)) {
                    if v == VENDOR_SENNHEISER && c == (GAIA_CMD_GET_ANC_STATUS | 0x0100) {
                        cfg.headset_mac = Some(self.mac.clone());
                        cfg.cached_channel = Some(ch);
                        save_config(&cfg);
                        return Ok(ch);
                    }
                }
                self.close();
            }
        }

        Err(io::Error::new(
            io::ErrorKind::ConnectionRefused,
            format!("Failed to connect to Sennheiser headset ({}) over RFCOMM channels", self.mac),
        ))
    }

    pub fn get_anc_status(&mut self) -> bool {
        if let Ok((v, _, p)) = self.exchange(GAIA_CMD_GET_ANC_STATUS, &[], Duration::from_millis(1500)) {
            if v == VENDOR_SENNHEISER && !p.is_empty() {
                return p[0] != 0;
            }
        }
        false
    }

    pub fn set_anc_status(&mut self, enabled: bool) -> bool {
        let payload = [if enabled { 1 } else { 0 }];
        if let Ok((v, _, _)) = self.exchange(GAIA_CMD_SET_ANC_STATUS, &payload, Duration::from_millis(1500)) {
            return v == VENDOR_SENNHEISER;
        }
        false
    }

    pub fn toggle_anc(&mut self) -> bool {
        let current = self.get_anc_status();
        let new_state = !current;
        self.set_anc_status(new_state);
        new_state
    }

    pub fn get_transparency(&mut self) -> i32 {
        if let Ok((v, _, p)) = self.exchange(GAIA_CMD_GET_TRANSPARENCY, &[], Duration::from_millis(1500)) {
            if v == VENDOR_SENNHEISER && !p.is_empty() {
                return p[0] as i32;
            }
        }
        0
    }

    pub fn set_transparency(&mut self, level: i32) -> bool {
        let lvl = level.clamp(0, 100) as u8;
        if let Ok((v, _, _)) = self.exchange(GAIA_CMD_SET_TRANSPARENCY, &[lvl], Duration::from_millis(1500)) {
            return v == VENDOR_SENNHEISER;
        }
        false
    }

    pub fn get_anc_strength(&mut self) -> i32 {
        if !self.get_anc_status() {
            return 0;
        }
        100 - self.get_transparency()
    }

    pub fn set_anc_strength(&mut self, strength: i32) -> bool {
        let s = strength.clamp(0, 100);
        if s > 0 {
            self.set_anc_status(true);
        }
        let trans = 100 - s;
        self.set_transparency(trans)
    }

    pub fn get_adaptive_anc(&mut self) -> bool {
        if let Ok((v, _, p)) = self.exchange(GAIA_CMD_GET_ADAPTIVE_ANC, &[], Duration::from_millis(1000)) {
            if v == VENDOR_SENNHEISER && !p.is_empty() {
                return p[0] != 0;
            }
        }
        false
    }

    pub fn set_adaptive_anc(&mut self, enabled: bool) -> bool {
        let payload = [if enabled { 1 } else { 0 }];
        if let Ok((v, _, _)) = self.exchange(GAIA_CMD_SET_ADAPTIVE_ANC, &payload, Duration::from_millis(1000)) {
            return v == VENDOR_SENNHEISER;
        }
        false
    }

    pub fn get_anti_wind(&mut self) -> String {
        if let Ok((v, _, p)) = self.exchange(GAIA_CMD_GET_ANTI_WIND, &[], Duration::from_millis(1000)) {
            if v == VENDOR_SENNHEISER && !p.is_empty() {
                return match p[0] {
                    1 => "auto",
                    2 => "max",
                    _ => "off",
                }
                .to_string();
            }
        }
        "off".to_string()
    }

    pub fn set_anti_wind(&mut self, mode: &str) -> bool {
        let val = match mode.to_lowercase().as_str() {
            "auto" => 1u8,
            "max" => 2u8,
            _ => 0u8,
        };
        if let Ok((v, _, _)) = self.exchange(GAIA_CMD_SET_ANTI_WIND, &[val], Duration::from_millis(1000)) {
            return v == VENDOR_SENNHEISER;
        }
        false
    }

    pub fn get_bass_boost(&mut self) -> bool {
        if let Ok((v, _, p)) = self.exchange(GAIA_CMD_GET_BASS_BOOST, &[], Duration::from_millis(1500)) {
            if v == VENDOR_SENNHEISER && !p.is_empty() {
                return p[0] != 0;
            }
        }
        false
    }

    pub fn set_bass_boost(&mut self, enabled: bool) -> bool {
        let payload = [if enabled { 1 } else { 0 }];
        if let Ok((v, _, _)) = self.exchange(GAIA_CMD_SET_BASS_BOOST, &payload, Duration::from_millis(1500)) {
            return v == VENDOR_SENNHEISER;
        }
        false
    }

    pub fn get_state(&mut self) -> HeadsetState {
        let anc = self.get_anc_status();
        let trans = self.get_transparency();
        HeadsetState {
            mac: self.mac.clone(),
            connected: self.is_connected(),
            anc_enabled: anc,
            anc_strength: if anc { 100 - trans } else { 0 },
            transparency: trans,
            adaptive_anc: self.get_adaptive_anc(),
            anti_wind: self.get_anti_wind(),
            bass_boost: self.get_bass_boost(),
            channel: self.channel,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_mac_to_bdaddr() {
        let mac = "00:11:22:33:44:55";
        let addr = parse_mac_to_bdaddr(mac);
        assert!(addr.is_some());
        // Little endian reverse order: 55:44:33:22:11:00
        assert_eq!(addr.unwrap(), [0x55, 0x44, 0x33, 0x22, 0x11, 0x00]);

        assert_eq!(parse_mac_to_bdaddr("invalid"), None);
        assert_eq!(parse_mac_to_bdaddr("00:11:22:33:44"), None);
        assert_eq!(parse_mac_to_bdaddr("00:11:22:33:44:55:66"), None);
        assert_eq!(parse_mac_to_bdaddr("00:11:22:33:44:ZZ"), None);
    }

    #[test]
    fn test_gaia_build() {
        let cmd = GAIA_CMD_SET_ANC_STATUS;
        let payload = [0x01];
        let pkt = GaiaProtocol::build(cmd, &payload);

        assert_eq!(&pkt[0..2], &GAIA_MAGIC);
        assert_eq!(u16::from_be_bytes([pkt[2], pkt[3]]), 1); // length
        assert_eq!(u16::from_be_bytes([pkt[4], pkt[5]]), VENDOR_SENNHEISER);
        assert_eq!(u16::from_be_bytes([pkt[6], pkt[7]]), cmd);
        assert_eq!(&pkt[8..], &[0x01]);
    }

    #[test]
    fn test_gaia_parse_many_single() {
        let pkt = GaiaProtocol::build(GAIA_CMD_GET_ANC_STATUS, &[0x01]);
        let (packets, consumed) = GaiaProtocol::parse_many(&pkt);
        assert_eq!(packets.len(), 1);
        assert_eq!(consumed, pkt.len());
        assert_eq!(packets[0].0, VENDOR_SENNHEISER);
        assert_eq!(packets[0].1, GAIA_CMD_GET_ANC_STATUS);
        assert_eq!(packets[0].2, vec![0x01]);
    }

    #[test]
    fn test_gaia_parse_many_multiple() {
        let mut buffer = Vec::new();
        buffer.extend_from_slice(&GaiaProtocol::build(0x1001, &[1, 2]));
        buffer.extend_from_slice(&GaiaProtocol::build(0x1002, &[3, 4, 5]));

        let (packets, consumed) = GaiaProtocol::parse_many(&buffer);
        assert_eq!(packets.len(), 2);
        assert_eq!(consumed, buffer.len());
        assert_eq!(packets[0].1, 0x1001);
        assert_eq!(packets[0].2, vec![1, 2]);
        assert_eq!(packets[1].1, 0x1002);
        assert_eq!(packets[1].2, vec![3, 4, 5]);
    }

    #[test]
    fn test_gaia_parse_many_with_garbage_prefix() {
        let mut buffer = vec![0x00, 0xAA, 0xBB];
        buffer.extend_from_slice(&GaiaProtocol::build(0x1A04, &[0x00]));

        let (packets, consumed) = GaiaProtocol::parse_many(&buffer);
        assert_eq!(packets.len(), 1);
        assert_eq!(consumed, buffer.len());
        assert_eq!(packets[0].1, 0x1A04);
        assert_eq!(packets[0].2, vec![0x00]);
    }

    #[test]
    fn test_headset_config_serde() {
        let cfg = HeadsetConfig {
            headset_mac: Some("AA:BB:CC:DD:EE:FF".to_string()),
            cached_channel: Some(2),
        };
        let json = serde_json::to_string(&cfg).unwrap();
        let loaded: HeadsetConfig = serde_json::from_str(&json).unwrap();
        assert_eq!(loaded.headset_mac.as_deref(), Some("AA:BB:CC:DD:EE:FF"));
        assert_eq!(loaded.cached_channel, Some(2));
    }
}

