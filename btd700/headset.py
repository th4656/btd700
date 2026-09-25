"""
Qualcomm GAIA Protocol client for Sennheiser Bluetooth Headphones (HDB 630, Momentum 4, Accentum, etc.).
Allows controlling Active Noise Cancellation (ANC), Transparency mode, and Bass Boost over Bluetooth RFCOMM.
"""

import os
import re
import json
import time
import socket
import select
import subprocess
import threading
from typing import Optional, Tuple, List, Dict, Any

# Qualcomm GAIA V3 Constants for Sennheiser
GAIA_MAGIC = b"\xFF\x03"
VENDOR_SENNHEISER = 0x0495

CMD_REGISTER_NOTIFICATION = 0x0007
CMD_SET_BASS_BOOST = 0x1008
CMD_GET_BASS_BOOST = 0x1009
CMD_SET_TRANSPARENCY = 0x1A02
CMD_GET_TRANSPARENCY = 0x1A03
CMD_SET_ANC_STATUS = 0x1A04
CMD_GET_ANC_STATUS = 0x1A05
CMD_SET_ADAPTIVE_ANC = 0x1A06
CMD_GET_ADAPTIVE_ANC = 0x1A07
CMD_SET_ANTI_WIND = 0x1A08
CMD_GET_ANTI_WIND = 0x1A09

FEATURE_USER_EQ = 8
FEATURE_TRANSPARENCY = 12
FEATURE_ANC = 13

COMMON_RFCOMM_CHANNELS = [2, 1, 14, 15, 12, 3, 4, 5, 6, 7, 8, 9, 10, 11]

CONFIG_DIR = os.path.expanduser("~/.config/btd700")
CONFIG_FILE = os.path.join(CONFIG_DIR, "config.json")


def load_config() -> Dict[str, Any]:
    if os.path.exists(CONFIG_FILE):
        try:
            with open(CONFIG_FILE, "r") as f:
                return json.load(f)
        except Exception:
            pass
    return {}


def save_config(cfg: Dict[str, Any]):
    os.makedirs(CONFIG_DIR, exist_ok=True)
    try:
        with open(CONFIG_FILE, "w") as f:
            json.dump(cfg, f, indent=2)
    except Exception:
        pass


def find_paired_headsets() -> List[Dict[str, str]]:
    """Scan for paired Sennheiser headphones via bluetoothctl or system tools."""
    results = []
    # 1. Try bluetoothctl devices
    try:
        proc = subprocess.run(
            ["bluetoothctl", "devices"],
            capture_output=True,
            text=True,
            timeout=2.0
        )
        if proc.returncode == 0:
            for line in proc.stdout.splitlines():
                # Format: Device XX:XX:XX:XX:XX:XX Name
                parts = line.strip().split(" ", 2)
                if len(parts) >= 3 and parts[0] == "Device":
                    mac = parts[1].upper()
                    name = parts[2]
                    # Check for Sennheiser or known headphone keywords
                    lower = name.lower()
                    is_match = any(k in lower for k in ["hdb", "630", "sennheiser", "momentum", "accentum", "hd 450", "hd 350", "epos"])
                    results.append({
                        "mac": mac,
                        "name": name,
                        "recommended": is_match
                    })
    except Exception:
        pass

    # Sort so recommended Sennheiser devices come first
    results.sort(key=lambda x: not x["recommended"])
    return results


def get_default_mac() -> Optional[str]:
    """Retrieve saved headset MAC or auto-detect from paired Bluetooth devices."""
    cfg = load_config()
    if cfg.get("headset_mac"):
        return cfg["headset_mac"].upper()

    devices = find_paired_headsets()
    for d in devices:
        if d.get("recommended"):
            return d["mac"]
    if devices:
        return devices[0]["mac"]
    return None


class GaiaProtocol:
    @staticmethod
    def build(command: int, payload: bytes = b"", vendor: int = VENDOR_SENNHEISER) -> bytes:
        return (
            GAIA_MAGIC
            + len(payload).to_bytes(2, "big")
            + vendor.to_bytes(2, "big")
            + command.to_bytes(2, "big")
            + payload
        )

    @staticmethod
    def expected_response(command: int) -> int:
        return command | 0x0100

    @staticmethod
    def parse_many(buffer: bytes) -> Tuple[List[Tuple[int, int, bytes]], bytes]:
        packets = []
        offset = 0
        while len(buffer) - offset >= 8:
            if buffer[offset:offset + 2] != GAIA_MAGIC:
                next_magic = buffer.find(GAIA_MAGIC, offset + 1)
                if next_magic == -1:
                    return packets, buffer[-1:]
                offset = next_magic
                continue
            payload_len = int.from_bytes(buffer[offset + 2:offset + 4], "big")
            total = 8 + payload_len
            if len(buffer) - offset < total:
                break
            vendor = int.from_bytes(buffer[offset + 4:offset + 6], "big")
            command = int.from_bytes(buffer[offset + 6:offset + 8], "big")
            payload = buffer[offset + 8:offset + total]
            packets.append((vendor, command, payload))
            offset += total
        return packets, buffer[offset:]


class SennheiserHeadset:
    """Controls Active Noise Cancellation (ANC), Transparency, and EQ on Sennheiser headsets."""

    def __init__(self, mac: Optional[str] = None):
        self.mac = (mac or get_default_mac() or "").upper()
        self.sock: Optional[socket.socket] = None
        self.channel: Optional[int] = None
        self.buffer = b""
        self.lock = threading.Lock()

    def is_connected(self) -> bool:
        return self.sock is not None

    def close(self):
        if self.sock is not None:
            try:
                self.sock.close()
            except OSError:
                pass
            self.sock = None
        self.channel = None
        self.buffer = b""

    def __enter__(self):
        self.connect()
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()

    def _connect_channel(self, channel: int, timeout: float = 3.0):
        self.close()
        sock = socket.socket(socket.AF_BLUETOOTH, socket.SOCK_STREAM, socket.BTPROTO_RFCOMM)
        sock.settimeout(timeout)
        sock.connect((self.mac, channel))
        self.sock = sock
        self.channel = channel

    def _recv_until(self, wanted_commands: set, timeout: float = 3.0) -> Tuple[int, int, bytes]:
        if self.sock is None:
            raise RuntimeError("Not connected to headset")
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            packets, self.buffer = GaiaProtocol.parse_many(self.buffer)
            for vendor, command, payload in packets:
                if command in wanted_commands:
                    return vendor, command, payload
            remaining = max(0.1, deadline - time.monotonic())
            self.sock.settimeout(remaining)
            try:
                chunk = self.sock.recv(4096)
                if not chunk:
                    raise ConnectionError("RFCOMM channel closed")
                self.buffer += chunk
            except (socket.timeout, TimeoutError):
                pass
        raise TimeoutError(f"Timed out waiting for response to {wanted_commands}")

    def _exchange(self, command: int, payload: bytes = b"", timeout: float = 3.0) -> Tuple[int, int, bytes]:
        if self.sock is None:
            raise RuntimeError("Not connected to headset")
        packet = GaiaProtocol.build(command, payload)
        with self.lock:
            self.sock.sendall(packet)
            wanted = {GaiaProtocol.expected_response(command), command | 0x0180}
            return self._recv_until(wanted, timeout=timeout)

    def connect(self, timeout: float = 2.5) -> int:
        """Connect to the headset over RFCOMM, searching standard GAIA channels."""
        if not self.mac:
            raise ValueError("No Bluetooth MAC address provided. Specify MAC or pair headset with Linux.")

        # Check if cached channel in config
        cfg = load_config()
        cached_channel = cfg.get(f"channel_{self.mac}")
        channels_to_try = COMMON_RFCOMM_CHANNELS
        if cached_channel and cached_channel in channels_to_try:
            channels_to_try = [cached_channel] + [c for c in channels_to_try if c != cached_channel]

        errors = []
        for ch in channels_to_try:
            try:
                self._connect_channel(ch, timeout=timeout)
                vendor, command, payload = self._exchange(CMD_GET_ANC_STATUS, timeout=1.8)
                if vendor == VENDOR_SENNHEISER and command == GaiaProtocol.expected_response(CMD_GET_ANC_STATUS):
                    # Cache successful channel & MAC
                    cfg["headset_mac"] = self.mac
                    cfg[f"channel_{self.mac}"] = ch
                    save_config(cfg)
                    return ch
            except Exception as e:
                errors.append(f"ch {ch}: {e}")
                self.close()

        raise ConnectionError(
            f"Failed to connect to Sennheiser headset ({self.mac}) over RFCOMM. "
            f"Ensure the headset is powered on, paired, and within range.\nDetails: {', '.join(errors[:3])}"
        )

    def get_anc_status(self) -> bool:
        """Query if Active Noise Cancellation is currently enabled."""
        vendor, command, payload = self._exchange(CMD_GET_ANC_STATUS)
        if vendor == VENDOR_SENNHEISER and command == GaiaProtocol.expected_response(CMD_GET_ANC_STATUS) and payload:
            return bool(payload[0])
        return False

    def set_anc_status(self, enabled: bool) -> bool:
        """Turn Active Noise Cancellation ON (True) or OFF (False)."""
        payload = bytes([1 if enabled else 0])
        vendor, command, resp_payload = self._exchange(CMD_SET_ANC_STATUS, payload)
        return vendor == VENDOR_SENNHEISER

    def toggle_anc(self) -> bool:
        """Toggle ANC between enabled and disabled, returning new state."""
        current = self.get_anc_status()
        new_state = not current
        self.set_anc_status(new_state)
        return new_state

    def get_transparency(self) -> int:
        """Get transparency level (0-100%)."""
        vendor, command, payload = self._exchange(CMD_GET_TRANSPARENCY)
        if vendor == VENDOR_SENNHEISER and payload:
            return int(payload[0])
        return 0

    def set_transparency(self, level: int) -> bool:
        """Set transparency level (0-100%)."""
        level = max(0, min(100, int(level)))
        vendor, command, payload = self._exchange(CMD_SET_TRANSPARENCY, bytes([level]))
        return vendor == VENDOR_SENNHEISER

    def get_bass_boost(self) -> bool:
        """Query if Bass Boost is enabled."""
        vendor, command, payload = self._exchange(CMD_GET_BASS_BOOST)
        if vendor == VENDOR_SENNHEISER and payload:
            return bool(payload[0])
        return False

    def set_bass_boost(self, enabled: bool) -> bool:
        """Set Bass Boost ON or OFF."""
        vendor, command, payload = self._exchange(CMD_SET_BASS_BOOST, bytes([1 if enabled else 0]))
        return vendor == VENDOR_SENNHEISER

    def get_anc_strength(self) -> int:
        """Get ANC strength percentage (0-100%). 100% is maximum noise cancellation."""
        if not self.get_anc_status():
            return 0
        return 100 - self.get_transparency()

    def set_anc_strength(self, strength: int) -> bool:
        """Set ANC strength percentage (0-100%). 100% = max noise cancellation, 0% = max transparency."""
        strength = max(0, min(100, int(strength)))
        if strength > 0:
            self.set_anc_status(True)
        transparency = 100 - strength
        return self.set_transparency(transparency)

    def get_adaptive_anc(self) -> bool:
        """Query if Adaptive ANC is enabled."""
        try:
            vendor, command, payload = self._exchange(CMD_GET_ADAPTIVE_ANC, timeout=1.5)
            if vendor == VENDOR_SENNHEISER and payload:
                return bool(payload[0])
        except Exception:
            pass
        return False

    def set_adaptive_anc(self, enabled: bool) -> bool:
        """Turn Adaptive ANC ON (auto adjusts) or OFF (fixed level)."""
        try:
            vendor, command, payload = self._exchange(CMD_SET_ADAPTIVE_ANC, bytes([1 if enabled else 0]), timeout=1.5)
            return vendor == VENDOR_SENNHEISER
        except Exception:
            return False

    def get_anti_wind(self) -> str:
        """Get anti-wind reduction mode ('off', 'auto', 'max')."""
        try:
            vendor, command, payload = self._exchange(CMD_GET_ANTI_WIND, timeout=1.5)
            if vendor == VENDOR_SENNHEISER and payload:
                val = payload[0]
                return {0: "off", 1: "auto", 2: "max"}.get(val, "off")
        except Exception:
            pass
        return "off"

    def set_anti_wind(self, mode: str) -> bool:
        """Set anti-wind reduction mode ('off', 'auto', 'max')."""
        m_map = {"off": 0, "auto": 1, "max": 2}
        val = m_map.get(mode.lower(), 0)
        try:
            vendor, command, payload = self._exchange(CMD_SET_ANTI_WIND, bytes([val]), timeout=1.5)
            return vendor == VENDOR_SENNHEISER
        except Exception:
            return False

    def get_state(self) -> Dict[str, Any]:
        """Fetch complete headset state (ANC, Strength, Transparency, Bass Boost, Wind)."""
        anc_on = self.get_anc_status()
        trans = self.get_transparency()
        return {
            "mac": self.mac,
            "connected": self.is_connected(),
            "anc_enabled": anc_on,
            "anc_strength": (100 - trans) if anc_on else 0,
            "transparency": trans,
            "adaptive_anc": self.get_adaptive_anc(),
            "anti_wind": self.get_anti_wind(),
            "bass_boost": self.get_bass_boost(),
            "channel": self.channel,
        }

