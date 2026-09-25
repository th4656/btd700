"""
Sennheiser Cloud Firmware Repository API Client
Queries available firmware releases, manifests, release notes, and downloads DFU binaries.
"""

import os
import json
import urllib.request
import urllib.error
from typing import List, Dict, Any, Optional, Callable

from .constants import API_BASE_URL, KNOWN_DEVICES


class SennheiserCloudClient:
    """Client for Sennheiser Cloud Firmware API v2."""

    def __init__(self, base_url: str = API_BASE_URL):
        self.base_url = base_url.rstrip("/") + "/"

    def _get_json(self, endpoint: str) -> Dict[str, Any]:
        url = self.base_url + endpoint
        req = urllib.request.Request(
            url,
            headers={
                "User-Agent": "SennheiserDongleControl/1.0.5 (Linux)",
                "Accept": "application/json",
            }
        )
        try:
            with urllib.request.urlopen(req, timeout=10) as resp:
                content = resp.read().decode("utf-8")
                return json.loads(content)
        except urllib.error.HTTPError as e:
            err_body = e.read().decode("utf-8", errors="ignore")
            raise RuntimeError(f"HTTP Error {e.code} for {url}: {err_body}")
        except urllib.error.URLError as e:
            raise RuntimeError(f"Network error querying {url}: {e.reason}")

    def get_available_versions(self, sku: str) -> List[str]:
        """
        Query available system releases for a product SKU.
        Example: BTD 700 is SKU '700434', BTD 600 is SKU '700248'.
        """
        data = self._get_json(f"availableSystemReleases/{sku}?os_type=android")
        if data.get("success") and "data" in data:
            return data["data"]
        return []

    def get_version_manifest(self, sku: str, version: str) -> Dict[str, Any]:
        """
        Get firmware packages manifest for a specific version.
        Contains the download URL for the .bin DFU file.
        """
        data = self._get_json(f"systemRelease/{sku}/{version}?os_type=android")
        if data.get("success") and "data" in data:
            return data["data"]
        raise ValueError(f"Failed to get manifest for SKU {sku} version {version}")

    def get_release_notes(self, sku: str, version: str, lang: str = "en") -> str:
        """Fetch release notes for a given version."""
        try:
            data = self._get_json(f"getExtras/{sku}/{version}?os_type=android")
            if data.get("success") and "data" in data:
                notes = data["data"].get("release_notes", {})
                return notes.get(lang) or notes.get("en") or ""
        except Exception:
            pass
        return ""

    def get_dfu_download_url(self, sku: str, version: str) -> Optional[str]:
        """Extract the .bin DFU download URL from the version manifest."""
        manifest = self.get_version_manifest(sku, version)
        packages = manifest.get("packages", [])
        for pkg in packages:
            url = pkg.get("url", "").strip()
            if url.upper().endswith(".BIN"):
                return url
        return None

    def download_firmware(
        self,
        url: str,
        dest_path: str,
        progress_cb: Optional[Callable[[int, int], None]] = None
    ) -> str:
        """Download firmware file with optional progress callback."""
        req = urllib.request.Request(
            url,
            headers={"User-Agent": "SennheiserDongleControl/1.0.5 (Linux)"}
        )
        with urllib.request.urlopen(req, timeout=30) as resp:
            total_size = int(resp.headers.get("content-length", 0))
            downloaded = 0
            chunk_size = 16384
            os.makedirs(os.path.dirname(os.path.abspath(dest_path)), exist_ok=True)
            with open(dest_path, "wb") as f:
                while True:
                    chunk = resp.read(chunk_size)
                    if not chunk:
                        break
                    f.write(chunk)
                    downloaded += len(chunk)
                    if progress_cb and total_size > 0:
                        progress_cb(downloaded, total_size)
        return dest_path
