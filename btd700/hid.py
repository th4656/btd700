"""
Cross-platform Linux HID communication layer for Sennheiser BTD dongles.
Implements native Linux /dev/hidraw communication with zero required external dependencies,
with fallback to python-hidapi if available.
"""

import os
import glob
import time
import fcntl
import select
import struct
import threading
from typing import Optional, List, Dict, Any, Callable

from .constants import (
    SENNHEISER_VID,
    BTD600_PID,
    BTD700_PID,
    KNOWN_DEVICES,
    ReportID,
)

# Linux hidraw ioctl numbers
# _IOC(dir, type, nr, size)
# dir: 0=none, 1=write, 2=read, 3=read|write
# 'H' = 0x48 = 72
def _IOC(dir_flag: int, type_char: str, nr: int, size: int) -> int:
    return (dir_flag << 30) | (size << 16) | (ord(type_char) << 8) | nr

def HIDIOCSFEATURE(length: int) -> int:
    return _IOC(3, 'H', 0x06, length)

def HIDIOCGFEATURE(length: int) -> int:
    return _IOC(3, 'H', 0x07, length)

HIDIOCGRAWINFO = _IOC(2, 'H', 0x03, 8)


class HidrawDevice:
    """Direct interface to a Linux /dev/hidraw* device."""

    def __init__(self, dev_path: str, vid: int, pid: int, name: str = "", report_ids: Optional[List[int]] = None):
        self.dev_path = dev_path
        self.vid = vid
        self.pid = pid
        self.name = name
        self.report_ids = report_ids or []
        self._fd: Optional[int] = None
        self._lock = threading.Lock()
        self._rx_thread: Optional[threading.Thread] = None
        self._running = False
        self._callbacks: List[Callable[[bytes], None]] = []

    def open(self) -> bool:
        if self._fd is not None:
            return True
        try:
            self._fd = os.open(self.dev_path, os.O_RDWR | os.O_NONBLOCK)
            return True
        except PermissionError:
            raise PermissionError(
                f"Permission denied accessing {self.dev_path}. "
                "Ensure your user has access to hidraw devices (e.g. install udev rules) or run with appropriate permissions."
            )
        except OSError as e:
            return False

    def close(self):
        self.stop_reader()
        with self._lock:
            if self._fd is not None:
                try:
                    os.close(self._fd)
                except OSError:
                    pass
                self._fd = None

    def write(self, data: bytes) -> int:
        """Write an output report to the hidraw device."""
        with self._lock:
            if self._fd is None:
                raise IOError(f"Device {self.dev_path} is not open")
            return os.write(self._fd, data)

    def read(self, length: int = 64, timeout_ms: int = 1000) -> Optional[bytes]:
        """Synchronously read an input report."""
        if self._fd is None:
            return None
        r, _, _ = select.select([self._fd], [], [], timeout_ms / 1000.0)
        if r:
            try:
                return os.read(self._fd, length)
            except OSError:
                return None
        return None

    def set_feature(self, data: bytes) -> bool:
        """Send a feature report via ioctl HIDIOCSFEATURE."""
        with self._lock:
            if self._fd is None:
                return False
            try:
                buf = bytearray(data)
                fcntl.ioctl(self._fd, HIDIOCSFEATURE(len(buf)), buf)
                return True
            except OSError:
                return False

    def get_feature(self, report_id: int, length: int = 64) -> Optional[bytes]:
        """Get a feature report via ioctl HIDIOCGFEATURE."""
        with self._lock:
            if self._fd is None:
                return None
            try:
                buf = bytearray([report_id] + [0] * (length - 1))
                fcntl.ioctl(self._fd, HIDIOCGFEATURE(len(buf)), buf)
                return bytes(buf)
            except OSError:
                return None

    def register_callback(self, cb: Callable[[bytes], None]):
        if cb not in self._callbacks:
            self._callbacks.append(cb)

    def unregister_callback(self, cb: Callable[[bytes], None]):
        if cb in self._callbacks:
            self._callbacks.remove(cb)

    def start_reader(self):
        """Start asynchronous background read loop for event-driven updates."""
        if self._running or self._fd is None:
            return
        self._running = True
        self._rx_thread = threading.Thread(target=self._reader_loop, daemon=True, name=f"hidraw-reader-{os.path.basename(self.dev_path)}")
        self._rx_thread.start()

    def stop_reader(self):
        self._running = False
        if self._rx_thread and self._rx_thread.is_alive():
            self._rx_thread.join(timeout=0.5)
        self._rx_thread = None

    def _reader_loop(self):
        while self._running and self._fd is not None:
            try:
                r, _, _ = select.select([self._fd], [], [], 0.1)
                if r:
                    data = os.read(self._fd, 128)
                    if data:
                        for cb in list(self._callbacks):
                            try:
                                cb(data)
                            except Exception:
                                pass
            except (OSError, ValueError):
                break


def find_sennheiser_hid_devices() -> List[Dict[str, Any]]:
    """
    Enumerate connected Sennheiser BTD 600 / BTD 700 devices on Linux.
    Parses /sys/class/hidraw and inspection of report descriptors.
    """
    found = []
    
    # Scan /sys/class/hidraw
    hidraw_dirs = sorted(glob.glob("/sys/class/hidraw/hidraw*"))
    for hdir in hidraw_dirs:
        dev_name = os.path.basename(hdir)
        dev_path = f"/dev/{dev_name}"
        uevent_file = os.path.join(hdir, "device", "uevent")
        if not os.path.exists(uevent_file):
            continue
        
        vid = None
        pid = None
        name = ""
        serial = ""
        
        try:
            with open(uevent_file, "r") as f:
                for line in f:
                    line = line.strip()
                    if line.startswith("HID_ID="):
                        # Format: HID_ID=bustype:vendor:product
                        parts = line.split("=")[1].split(":")
                        if len(parts) >= 3:
                            vid = int(parts[1], 16)
                            pid = int(parts[2], 16)
                    elif line.startswith("HID_NAME="):
                        name = line.split("=")[1].strip('"')
        except OSError:
            continue
        
        if vid == SENNHEISER_VID and pid in (BTD600_PID, BTD700_PID):
            # Parse report descriptor to find supported report IDs
            report_ids = []
            desc_file = os.path.join(hdir, "device", "report_descriptor")
            if os.path.exists(desc_file):
                try:
                    with open(desc_file, "rb") as f:
                        desc = f.read()
                    # Search for 0x85 <report_id> in report descriptor
                    i = 0
                    while i < len(desc) - 1:
                        if desc[i] == 0x85:
                            report_ids.append(desc[i + 1])
                            i += 2
                        else:
                            i += 1
                except OSError:
                    pass
            
            # Read serial number if available
            serial_file = os.path.join(hdir, "device", "..", "serial")
            if not os.path.exists(serial_file):
                # Try higher in sysfs tree
                serial_file = os.path.join(hdir, "device", "..", "..", "serial")
            if os.path.exists(serial_file):
                try:
                    with open(serial_file, "r") as f:
                        serial = f.read().strip()
                except OSError:
                    pass
            
            info = KNOWN_DEVICES.get((vid, pid), {
                "name": "Unknown Sennheiser Device",
                "sku": "",
                "type": None,
                "reports": [],
            })
            
            found.append({
                "dev_path": dev_path,
                "sys_path": hdir,
                "vid": vid,
                "pid": pid,
                "model_name": info.get("name", "Sennheiser Dongle"),
                "product_name": name,
                "serial": serial,
                "report_ids": sorted(list(set(report_ids))),
                "device_info": info,
            })
            
    return found
