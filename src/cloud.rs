//! Sennheiser Cloud Firmware API client.

use std::fs::File;
use std::io;
use std::path::Path;
use serde_json::Value;

pub const CLOUD_BASE_URL: &str = "https://api.s-consumer-cloud.com/firmware/api/v2";

pub struct SennheiserCloudClient {
    base_url: String,
}

impl Default for SennheiserCloudClient {
    fn default() -> Self {
        Self {
            base_url: CLOUD_BASE_URL.to_string(),
        }
    }
}

impl SennheiserCloudClient {
    pub fn new() -> Self {
        Self::default()
    }

    pub fn get_available_versions(&self, sku: &str) -> Result<Vec<String>, String> {
        let url = format!("{}/availableSystemReleases/{}?os_type=android", self.base_url, sku);
        let resp = ureq::get(&url)
            .call()
            .map_err(|e| format!("HTTP request failed: {e}"))?;

        let body: Value = resp
            .into_json()
            .map_err(|e| format!("Failed to parse JSON: {e}"))?;

        let mut versions = Vec::new();
        if let Some(arr) = body.as_array() {
            for item in arr {
                if let Some(v) = item.get("systemRelease").and_then(|v| v.as_str()) {
                    versions.push(v.to_string());
                }
            }
        }
        Ok(versions)
    }

    pub fn get_release_notes(&self, sku: &str, version: &str) -> Result<String, String> {
        let url = format!("{}/getExtras/{}/{}?os_type=android", self.base_url, sku, version);
        let resp = ureq::get(&url)
            .call()
            .map_err(|e| format!("HTTP request failed: {e}"))?;

        let body: Value = resp
            .into_json()
            .map_err(|e| format!("Failed to parse JSON: {e}"))?;

        if let Some(notes) = body.get("releaseNotes").and_then(|v| v.as_str()) {
            return Ok(notes.to_string());
        }
        Ok(String::new())
    }

    pub fn get_dfu_download_url(&self, sku: &str, version: &str) -> Result<String, String> {
        let url = format!("{}/systemRelease/{}/{}?os_type=android", self.base_url, sku, version);
        let resp = ureq::get(&url)
            .call()
            .map_err(|e| format!("HTTP request failed: {e}"))?;

        let body: Value = resp
            .into_json()
            .map_err(|e| format!("Failed to parse JSON: {e}"))?;

        if let Some(items) = body.get("items").and_then(|v| v.as_array()) {
            for it in items {
                if let Some(u) = it.get("url").and_then(|v| v.as_str()) {
                    return Ok(u.to_string());
                }
            }
        }
        Err("No download URL found in release manifest".to_string())
    }

    pub fn download_firmware(&self, url: &str, dest: &Path) -> Result<(), String> {
        let resp = ureq::get(url)
            .call()
            .map_err(|e| format!("Failed to download firmware: {e}"))?;

        let mut reader = resp.into_reader();
        let mut file = File::create(dest).map_err(|e| format!("Failed to create file: {e}"))?;
        io::copy(&mut reader, &mut file).map_err(|e| format!("Failed to write file: {e}"))?;
        Ok(())
    }
}
