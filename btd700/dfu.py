"""
Qualcomm QCC UPM DFU firmware file parser and flasher for Sennheiser BTD 600 / BTD 700.
"""

import os
import struct
import hashlib
import zlib
from typing import List, Dict, Any, Optional, Tuple, Callable


class PartitionHeader:
    def __init__(self, length: int, part_type: int, number: int):
        self.length = length
        self.type = part_type
        self.number = number


class DFUImage:
    """Parses and validates a Qualcomm APPUHDR5 DFU firmware binary."""

    def __init__(self, filepath: str):
        self.filepath = filepath
        self.is_valid = False
        self.error = ""
        self.chip_model = ""
        self.firmware_version = ""
        self.config_version = 0
        self.file_signature: bytes = b""
        self.partitions: List[PartitionHeader] = []
        self._data: bytes = b""
        self._load()

    def _load(self):
        if not os.path.exists(self.filepath):
            self.error = f"File not found: {self.filepath}"
            return

        with open(self.filepath, "rb") as f:
            self._data = f.read()

        # Compute Qualcomm file signature: CRC32 of MD5
        md5_digest = hashlib.md5(self._data).digest()
        crc = zlib.crc32(md5_digest) & 0xFFFFFFFF
        self.file_signature = struct.pack("<I", crc)

        if len(self._data) < 32:
            self.error = "File too small"
            return

        pos = 0
        fixed_id = self._data[pos:pos+8]
        pos += 8
        if fixed_id != b"APPUHDR5":
            self.error = f"Invalid magic header: {fixed_id.decode('ascii', errors='replace')} (expected APPUHDR5)"
            return

        header_len = struct.unpack(">I", self._data[pos:pos+4])[0]
        pos += 4

        dev_variant = self._data[pos:pos+8].decode("ascii", errors="replace").strip("\x00")
        pos += 8
        self.chip_model = dev_variant

        ver_raw = struct.unpack(">I", self._data[pos:pos+4])[0]
        pos += 4
        major = ver_raw >> 16
        minor = (ver_raw & 0xFFFF) // 100
        test = (ver_raw & 0xFFFF) % 100
        self.firmware_version = f"{major}.{minor}.{test}"

        num_comp_ver = struct.unpack(">H", self._data[pos:pos+2])[0]
        pos += 2
        pos += num_comp_ver * 4

        self.config_version = struct.unpack(">H", self._data[pos:pos+2])[0]
        pos += 2

        num_comp_cfg = struct.unpack(">H", self._data[pos:pos+2])[0]
        pos += 2
        pos += num_comp_cfg * 2

        # Advance to end of header
        pos = header_len + 12

        # Parse partitions
        while pos + 12 <= len(self._data):
            sec_id = self._data[pos:pos+8]
            pos += 8
            if sec_id == b"PARTDATA":
                part_len = struct.unpack(">I", self._data[pos:pos+4])[0]
                pos += 4
                part_type = struct.unpack(">H", self._data[pos:pos+2])[0]
                part_num = struct.unpack(">H", self._data[pos+2:pos+4])[0]
                pos += part_len
                self.partitions.append(PartitionHeader(part_len, part_type, part_num))
            elif sec_id == b"APPUPFTR":
                self.is_valid = True
                self.error = ""
                return
            else:
                break

        if not self.is_valid:
            self.error = "APPUPFTR footer not found"

    @property
    def size(self) -> int:
        return len(self._data)

    def get_data(self) -> bytes:
        return self._data
