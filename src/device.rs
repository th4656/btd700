//! BTD 600 / BTD 700 device communication abstraction.

use std::time::{Duration, Instant};
use serde::{Deserialize, Serialize};

use crate::constants::*;
use crate::hid::{find_sennheiser_hid_devices, DeviceInfo, HidrawDevice};
use crate::protocol::Protocol;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CodecInfo {
    pub name: String,
    pub bit: u8,
    pub mask: u32,
    pub is_active: bool,
}

#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct DongleStatus {
    pub connected: bool,
    pub model: String,
    pub serial: String,
    pub vid: String,
    pub pid: String,
    pub dongle_state: String,
    pub dongle_state_code: u8,
    pub audio_mode: String,
    pub audio_mode_code: u8,
    pub transport_mode: String,
    pub transport_mode_code: u8,
    pub connected_transport: String,
    pub frequency: String,
    pub resolution: String,
    pub codec_in_use: String,
    pub codec_in_use_bit: Option<u8>,
    pub supported_codecs: Vec<CodecInfo>,
    pub broadcast_enabled: bool,
    pub broadcast_quality: String,
    pub broadcast_quality_code: u8,
    pub broadcast_name: String,
    pub broadcast_encrypted: bool,
}

pub struct BTDDevice {
    pub hid: HidrawDevice,
    pub is_btd700: bool,
    last_status: DongleStatus,
}

impl BTDDevice {
    pub fn new(info: DeviceInfo) -> Self {
        let is_btd700 = info.pid == PID_BTD700;
        Self {
            hid: HidrawDevice::new(info),
            is_btd700,
            last_status: DongleStatus::default(),
        }
    }

    pub fn open(&mut self) -> std::io::Result<()> {
        self.hid.open()
    }

    pub fn close(&mut self) {
        self.hid.close()
    }

    pub fn send_command(
        &mut self,
        cmd: HostCmd,
        args: &[u8],
        timeout_ms: u64,
    ) -> Option<(u8, Vec<u8>)> {
        let packet = Protocol::build_host_cmd(cmd, args);
        if self.hid.write_report(&packet).is_err() {
            return None;
        }

        let start = Instant::now();
        let expected_cmd = cmd as u8;
        let timeout = Duration::from_millis(timeout_ms);

        while start.elapsed() < timeout {
            if let Ok(Some(raw)) = self.hid.read_report(20) {
                if let Some((resp_cmd, payload)) = Protocol::parse_response(&raw) {
                    if resp_cmd == expected_cmd || (expected_cmd == 1 && resp_cmd == 1) || (expected_cmd == 21 && resp_cmd == 21) {
                        return Some((resp_cmd, payload));
                    }
                }
            }
        }

        None
    }

    pub fn get_status(&mut self) -> DongleStatus {
        let mut st = DongleStatus {
            connected: true,
            model: self.hid.info.model_name.clone(),
            serial: self.hid.info.serial.clone(),
            vid: format!("0x{:04X}", self.hid.info.vid),
            pid: format!("0x{:04X}", self.hid.info.pid),
            ..Default::default()
        };

        if !self.is_btd700 {
            self.last_status = st.clone();
            return st;
        }

        const CMD_TIMEOUT: u64 = 80;

        // 1. Dongle State
        if let Some((_, p)) = self.send_command(HostCmd::GetDongleState, &[], CMD_TIMEOUT) {
            if !p.is_empty() {
                st.dongle_state_code = p[0];
                st.dongle_state = dongle_state_str(p[0]).to_string();
            }
        }

        // 2. Audio Mode & Transport
        if let Some((_, p)) = self.send_command(HostCmd::GetAudioModeAndTransport, &[], CMD_TIMEOUT) {
            if p.len() >= 2 {
                let am = AudioMode::from_u8(p[0]);
                let tm = TransportMode::from_u8(p[1]);
                st.audio_mode_code = p[0];
                st.audio_mode = am.as_str().to_string();
                st.transport_mode_code = p[1];
                st.transport_mode = tm.as_str().to_string();
                if p.len() >= 3 {
                    st.connected_transport = TransportMode::from_u8(p[2]).as_str().to_string();
                } else {
                    st.connected_transport = st.transport_mode.clone();
                }
            }
        }

        // 3. Audio Quality
        if let Some((_, p)) = self.send_command(HostCmd::GetAudioQuality, &[], CMD_TIMEOUT) {
            if p.len() >= 2 {
                st.frequency = audio_frequency_str(p[0]).to_string();
                st.resolution = audio_resolution_str(p[1]).to_string();
            }
        }

        // 4. Codec in use
        let mut active_bit = None;
        if let Some((_, p)) = self.send_command(HostCmd::GetCodecInUse, &[], CMD_TIMEOUT) {
            if !p.is_empty() {
                let mask = p[0];
                for b in 0..6 {
                    if (mask & (1 << b)) != 0 {
                        active_bit = Some(b);
                        if let Some(cb) = CodecBit::from_bit(b) {
                            st.codec_in_use = cb.name().to_string();
                            st.codec_in_use_bit = Some(b);
                        }
                        break;
                    }
                }
            }
        }

        // 5. Supported Codecs
        if let Some((_, p)) = self.send_command(HostCmd::GetSupportedCodec, &[], CMD_TIMEOUT) {
            if !p.is_empty() {
                let mask = p[0];
                let mut list = Vec::new();
                for b in 0..6 {
                    if (mask & (1 << b)) != 0 {
                        if let Some(cb) = CodecBit::from_bit(b) {
                            list.push(CodecInfo {
                                name: cb.name().to_string(),
                                bit: b,
                                mask: 1 << b,
                                is_active: Some(b) == active_bit,
                            });
                        }
                    }
                }
                st.supported_codecs = list;
            }
        }

        // 6. Broadcast Info
        if let Some((_, p)) = self.send_command(HostCmd::GetBroadcastInfo, &[], CMD_TIMEOUT) {
            if p.len() >= 3 {
                st.broadcast_enabled = p[0] == 1;
                st.broadcast_quality_code = p[1];
                st.broadcast_quality = BroadcastQuality::from_u8(p[1]).as_str().to_string();
                st.broadcast_encrypted = p[2] == 1;
            }
        }

        // 7. Broadcast Name
        if let Some((_, p)) = self.send_command(HostCmd::GetBroadcastName, &[], CMD_TIMEOUT) {
            st.broadcast_name = String::from_utf8_lossy(&p).to_string();
        }

        self.last_status = st.clone();
        st
    }

    pub fn set_audio_mode(&mut self, mode: AudioMode) -> bool {
        if !self.is_btd700 {
            return false;
        }
        let transport = self.last_status.transport_mode_code.max(1);
        let args = [mode as u8, transport];
        self.send_command(HostCmd::SetAudioModeAndTransport, &args, 1000).is_some()
    }

    pub fn set_codec(&mut self, codec_bit: u8) -> bool {
        if !self.is_btd700 {
            return false;
        }
        let mask = 1u8 << codec_bit;
        self.send_command(HostCmd::SetCodecToUse, &[mask], 1000).is_some()
    }

    pub fn set_broadcast_config(
        &mut self,
        enable: Option<bool>,
        name: Option<&str>,
        quality: Option<BroadcastQuality>,
        key: Option<&str>,
    ) -> bool {
        if !self.is_btd700 {
            return false;
        }

        if let Some(n) = name {
            self.send_command(HostCmd::SetBroadcastName, n.as_bytes(), 800);
        }

        if let Some(k) = key {
            self.send_command(HostCmd::SetBroadcastEncryptKey, k.as_bytes(), 800);
        }

        let curr_info = self.send_command(HostCmd::GetBroadcastInfo, &[], 500);
        let mut st_val = 0u8;
        let mut qual_val = 2u8;
        let mut enc_val = 0u8;

        if let Some((_, p)) = curr_info {
            if p.len() >= 3 {
                st_val = p[0];
                qual_val = p[1];
                enc_val = p[2];
            }
        }

        if let Some(en) = enable {
            st_val = if en { 1 } else { 0 };
        }
        if let Some(q) = quality {
            qual_val = q as u8;
        }
        if let Some(k) = key {
            enc_val = if !k.is_empty() { 1 } else { 0 };
        }

        let info = [st_val, qual_val, enc_val];
        self.send_command(HostCmd::SetBroadcastInfo, &info, 1000).is_some()
    }

    pub fn trigger_pairing(&mut self) -> bool {
        self.send_command(HostCmd::SetBTConnect, &[1], 1000).is_some()
    }

    pub fn factory_reset(&mut self) -> bool {
        self.send_command(HostCmd::SetFactoryReset, &[], 1000).is_some()
    }
}

pub fn get_first_dongle() -> Option<BTDDevice> {
    let devs = find_sennheiser_hid_devices();
    if devs.is_empty() {
        return None;
    }
    Some(BTDDevice::new(devs[0].clone()))
}
