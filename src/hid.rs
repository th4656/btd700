//! Linux /dev/hidraw interface for Sennheiser USB dongles.

use std::fs::{self, File, OpenOptions};
use std::io::{self, Write};
use std::os::unix::fs::OpenOptionsExt;
use std::os::unix::io::AsRawFd;
use std::path::Path;
use serde::{Deserialize, Serialize};

use crate::constants::{PID_BTD600, PID_BTD700, VID_SENNHEISER};

const fn ioc(dir: u32, type_char: u8, nr: u32, size: u32) -> u64 {
    ((dir as u64) << 30) | ((size as u64) << 16) | ((type_char as u64) << 8) | (nr as u64)
}

#[inline]
pub fn hidiocsfeature(len: usize) -> u64 {
    ioc(3, b'H', 0x06, len as u32)
}

#[inline]
pub fn hidiocgfeature(len: usize) -> u64 {
    ioc(3, b'H', 0x07, len as u32)
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DeviceInfo {
    pub dev_path: String,
    pub sys_path: String,
    pub vid: u16,
    pub pid: u16,
    pub model_name: String,
    pub product_name: String,
    pub serial: String,
    pub report_ids: Vec<u8>,
}

pub fn find_sennheiser_hid_devices() -> Vec<DeviceInfo> {
    let mut found = Vec::new();
    let sys_hidraw = Path::new("/sys/class/hidraw");
    if !sys_hidraw.exists() {
        return found;
    }

    let entries = match fs::read_dir(sys_hidraw) {
        Ok(e) => e,
        Err(_) => return found,
    };

    for entry in entries.flatten() {
        let hdir = entry.path();
        let dev_name = hdir.file_name().and_then(|n| n.to_str()).unwrap_or("");
        let dev_path = format!("/dev/{dev_name}");

        let uevent_file = hdir.join("device").join("uevent");
        if !uevent_file.exists() {
            continue;
        }

        let mut vid = 0u16;
        let mut pid = 0u16;
        let mut name = String::new();

        if let Ok(content) = fs::read_to_string(&uevent_file) {
            for line in content.lines() {
                if let Some(rest) = line.strip_prefix("HID_ID=") {
                    let parts: Vec<&str> = rest.split(':').collect();
                    if parts.len() >= 3 {
                        vid = u16::from_str_radix(parts[1], 16).unwrap_or(0);
                        pid = u16::from_str_radix(parts[2], 16).unwrap_or(0);
                    }
                } else if let Some(rest) = line.strip_prefix("HID_NAME=") {
                    name = rest.trim_matches('"').to_string();
                }
            }
        }

        if vid == VID_SENNHEISER && (pid == PID_BTD600 || pid == PID_BTD700) {
            let mut report_ids = Vec::new();
            let desc_file = hdir.join("device").join("report_descriptor");
            if let Ok(bytes) = fs::read(&desc_file) {
                let mut i = 0;
                while i < bytes.len().saturating_sub(1) {
                    if bytes[i] == 0x85 {
                        report_ids.push(bytes[i + 1]);
                        i += 2;
                    } else {
                        i += 1;
                    }
                }
            }
            report_ids.sort_unstable();
            report_ids.dedup();

            let mut serial = String::new();
            let candidates = [
                hdir.join("device").join("..").join("serial"),
                hdir.join("device").join("..").join("..").join("serial"),
            ];
            for cand in &candidates {
                if cand.exists() {
                    if let Ok(s) = fs::read_to_string(cand) {
                        serial = s.trim().to_string();
                        break;
                    }
                }
            }

            let model_name = match pid {
                PID_BTD600 => "Sennheiser BTD 600",
                PID_BTD700 => "Sennheiser BTD 700",
                _ => "Sennheiser Dongle",
            }
            .to_string();

            found.push(DeviceInfo {
                dev_path,
                sys_path: hdir.to_string_lossy().to_string(),
                vid,
                pid,
                model_name,
                product_name: name,
                serial,
                report_ids,
            });
        }
    }

    found
}

pub struct HidrawDevice {
    pub info: DeviceInfo,
    file: Option<File>,
}

impl HidrawDevice {
    pub fn new(info: DeviceInfo) -> Self {
        Self { info, file: None }
    }

    pub fn open(&mut self) -> io::Result<()> {
        let file = OpenOptions::new()
            .read(true)
            .write(true)
            .custom_flags(libc::O_NONBLOCK)
            .open(&self.info.dev_path)?;
        self.file = Some(file);
        Ok(())
    }

    pub fn close(&mut self) {
        self.file = None;
    }

    pub fn is_open(&self) -> bool {
        self.file.is_some()
    }

    pub fn write_report(&mut self, data: &[u8]) -> io::Result<usize> {
        let res = {
            let f = self.file.as_mut().ok_or_else(|| {
                io::Error::new(io::ErrorKind::NotConnected, "Device is not open")
            })?;
            f.write(data)
        };

        match res {
            Ok(n) => Ok(n),
            Err(e) => {
                // Fallback to HIDIOCSFEATURE ioctl if standard write fails on some kernels/interfaces
                if self.set_feature(data).is_ok() {
                    Ok(data.len())
                } else {
                    Err(e)
                }
            }
        }
    }

    pub fn read_report(&mut self, timeout_ms: u64) -> io::Result<Option<Vec<u8>>> {
        let f = self.file.as_mut().ok_or_else(|| {
            io::Error::new(io::ErrorKind::NotConnected, "Device is not open")
        })?;
        let fd = f.as_raw_fd();

        let mut pfd = libc::pollfd {
            fd,
            events: libc::POLLIN,
            revents: 0,
        };

        let ret = unsafe { libc::poll(&mut pfd, 1, timeout_ms as libc::c_int) };
        if ret <= 0 {
            return Ok(None);
        }

        if (pfd.revents & libc::POLLIN) != 0 {
            let mut buf = vec![0u8; 128];
            let n = unsafe {
                libc::read(fd, buf.as_mut_ptr() as *mut libc::c_void, buf.len())
            };
            if n > 0 {
                buf.truncate(n as usize);
                return Ok(Some(buf));
            }
        }

        Ok(None)
    }

    pub fn set_feature(&mut self, data: &[u8]) -> io::Result<()> {
        let f = self.file.as_mut().ok_or_else(|| {
            io::Error::new(io::ErrorKind::NotConnected, "Device is not open")
        })?;
        let fd = f.as_raw_fd();
        let cmd = hidiocsfeature(data.len());
        let ret = unsafe {
            libc::ioctl(fd, cmd as libc::c_ulong, data.as_ptr())
        };
        if ret < 0 {
            return Err(io::Error::last_os_error());
        }
        Ok(())
    }

    pub fn get_feature(&mut self, report_id: u8, len: usize) -> io::Result<Vec<u8>> {
        let f = self.file.as_mut().ok_or_else(|| {
            io::Error::new(io::ErrorKind::NotConnected, "Device is not open")
        })?;
        let fd = f.as_raw_fd();
        let mut buf = vec![0u8; len];
        buf[0] = report_id;
        let cmd = hidiocgfeature(len);
        let ret = unsafe {
            libc::ioctl(fd, cmd as libc::c_ulong, buf.as_mut_ptr())
        };
        if ret < 0 {
            return Err(io::Error::last_os_error());
        }
        Ok(buf)
    }
}
