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
                let end = bytes[pos..].iter().position(|&b| b == 0 || !(0x20..=0x7E).contains(&b)).unwrap_or(16);
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

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Write;

    #[test]
    fn test_dfu_too_small() {
        let temp = tempfile_named("small.bin", &[1, 2, 3]);
        let res = DFUImage::load(&temp);
        assert!(res.is_err());
        assert!(res.unwrap_err().contains("File too small"));
        let _ = std::fs::remove_file(temp);
    }

    #[test]
    fn test_dfu_appuhdr5_parsing() {
        let mut data = vec![0u8; 64];
        data[0..8].copy_from_slice(b"APPUHDR5");
        let ver = b"2.5.12";
        data[16..16 + ver.len()].copy_from_slice(ver);

        let temp = tempfile_named("firmware.bin", &data);
        let res = DFUImage::load(&temp);
        assert!(res.is_ok());
        let img = res.unwrap();
        assert_eq!(img.chip_model, "Qualcomm APPUHDR5 (QCC518x)");
        assert_eq!(img.firmware_version, "2.5.12");
        assert_eq!(img.data.len(), 64);
        let _ = std::fs::remove_file(temp);
    }

    fn tempfile_named(name: &str, content: &[u8]) -> std::path::PathBuf {
        let p = std::env::temp_dir().join(format!("btd700_test_{}_{}", std::process::id(), name));
        let mut f = std::fs::File::create(&p).unwrap();
        f.write_all(content).unwrap();
        p
    }
}

