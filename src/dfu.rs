//! Qualcomm Universal Programming Module (UPM) DFU flasher.

use std::fs;
use std::path::Path;

#[derive(Debug, Clone)]
pub struct DFUImage {
    pub path: String,
    pub firmware_version: String,
    pub chip_model: String,
    pub data: Vec<u8>,
}

impl DFUImage {
    pub fn load<P: AsRef<Path>>(path: P) -> Result<Self, String> {
        let p_str = path.as_ref().to_string_lossy().to_string();
        let bytes = fs::read(&path).map_err(|e| format!("Failed to read file: {e}"))?;

        if bytes.len() < 32 {
            return Err("File too small to be a valid DFU image".to_string());
        }

        let mut version = "Unknown".to_string();
        let mut chip = "Qualcomm QCC518x/QCC515x".to_string();

        if &bytes[0..8] == b"APPUHDR5" {
            chip = "Qualcomm APPUHDR5 (QCC518x)".to_string();
            // Search for version string
            if let Some(pos) = bytes.windows(4).position(|w| w == b"2.5." || w == b"2.4." || w == b"1.2." || w == b"3.0.") {
                let end = bytes[pos..].iter().position(|&b| b == 0 || b < 0x20 || b > 0x7E).unwrap_or(16);
                version = String::from_utf8_lossy(&bytes[pos..pos + end]).to_string();
            }
        }

        Ok(Self {
            path: p_str,
            firmware_version: version,
            chip_model: chip,
            data: bytes,
        })
    }
}
