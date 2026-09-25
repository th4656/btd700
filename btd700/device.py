"""
High-level Sennheiser BTD Dongle Device Controller.
Provides structured commands to inspect and configure the dongle.
"""

import time
import threading
from typing import Optional, Dict, Any, List, Tuple, Callable

from .constants import (
    SENNHEISER_VID,
    BTD600_PID,
    BTD700_PID,
    ReportID,
    HostCmd,
    DongleCmd,
    AudioMode,
    TransportMode,
    SinkMode,
    CodecBit,
    CODEC_NAMES,
    DongleState,
    LEAudioState,
    AudioFrequency,
    AudioResolution,
    BroadcastState,
    BroadcastQuality,
    BroadcastEncryption,
    DeviceType,
    FREQ_LABELS,
    RES_LABELS,
    QUALITY_LABELS,
)
from .protocol import (
    build_host_command,
    parse_dongle_response,
    decode_audio_mode_and_transport,
    decode_supported_codecs,
    decode_codec_in_use,
    decode_dongle_state,
    decode_le_audio_state,
    decode_audio_quality,
    decode_broadcast_info,
    decode_sink_support_transport,
)
from .hid import HidrawDevice, find_sennheiser_hid_devices


class BTDDevice:
    """Represents a connected Sennheiser BTD 600 or BTD 700 dongle."""

    def __init__(self, dev_info: Dict[str, Any]):
        self.dev_info = dev_info
        self.model_name = dev_info.get("model_name", "BTD Dongle")
        self.serial = dev_info.get("serial", "")
        self.vid = dev_info.get("vid", SENNHEISER_VID)
        self.pid = dev_info.get("pid", BTD700_PID)
        self.is_btd700 = (self.pid == BTD700_PID)
        
        # Primary control HID interface (handles Report ID 52 for BTD 700)
        self.control_dev = HidrawDevice(
            dev_info["dev_path"],
            self.vid,
            self.pid,
            name=self.model_name,
            report_ids=dev_info.get("report_ids", [])
        )
        self._lock = threading.Lock()
        self._last_state: Dict[str, Any] = {}

    def open(self) -> bool:
        return self.control_dev.open()

    def close(self):
        self.control_dev.close()

    def __enter__(self):
        self.open()
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()

    def send_command(self, cmd: HostCmd, args: Optional[bytes] = None, wait_resp: bool = True, timeout_ms: int = 1500) -> Optional[Tuple[int, bytes]]:
        """
        Send a BTD 700 Host Command and wait for response.
        """
        with self._lock:
            packet = build_host_command(cmd, args)
            self.control_dev.write(packet)
            
            if not wait_resp:
                return None
            
            start_t = time.time()
            while (time.time() - start_t) * 1000 < timeout_ms:
                raw = self.control_dev.read(64, timeout_ms=300)
                if not raw:
                    continue
                resp = parse_dongle_response(raw)
                if resp:
                    resp_cmd, resp_payload = resp
                    # Verify if response matches command or notification
                    if resp_cmd == int(cmd):
                        return resp
                    # Some commands map to specific response IDs
                    if cmd == HostCmd.GET_AUDIO_MODE_AND_TRANSPORT and resp_cmd == 1:
                        return resp
                    if cmd == HostCmd.GET_SINK_SUPPORT_TRANSPORT and resp_cmd == 21:
                        return resp
            return None

    def get_status(self) -> Dict[str, Any]:
        """Query comprehensive status of the dongle."""
        status: Dict[str, Any] = {
            "model": self.model_name,
            "serial": self.serial,
            "vid": f"0x{self.vid:04X}",
            "pid": f"0x{self.pid:04X}",
            "connected": True,
        }

        if not self.is_btd700:
            # BTD 600 only supports DFU / firmware update via UPM
            return status

        # 1. Dongle State
        resp = self.send_command(HostCmd.GET_DONGLE_STATE)
        if resp:
            state = decode_dongle_state(resp[1])
            status["dongle_state"] = state.name
            status["dongle_state_code"] = state.value

        # 2. Audio Mode & Transport
        resp = self.send_command(HostCmd.GET_AUDIO_MODE_AND_TRANSPORT)
        if resp:
            mode_info = decode_audio_mode_and_transport(resp[1])
            status["audio_mode"] = mode_info["audio_mode"].name if isinstance(mode_info["audio_mode"], AudioMode) else str(mode_info["audio_mode"])
            status["audio_mode_code"] = int(mode_info["audio_mode"])
            status["transport_mode"] = mode_info["transport_mode"].name if isinstance(mode_info["transport_mode"], TransportMode) else str(mode_info["transport_mode"])
            status["transport_mode_code"] = int(mode_info["transport_mode"])
            if "connected_transport" in mode_info:
                ct = mode_info["connected_transport"]
                status["connected_transport"] = ct.name if isinstance(ct, TransportMode) else str(ct)

        # 3. Audio Quality (Sample Rate & Bit Depth)
        resp = self.send_command(HostCmd.GET_AUDIO_QUALITY)
        if resp:
            quality_info = decode_audio_quality(resp[1])
            freq = quality_info["frequency"]
            res = quality_info["resolution"]
            status["frequency"] = FREQ_LABELS.get(freq, "Unknown")
            status["frequency_code"] = freq.value if freq else 0
            status["resolution"] = RES_LABELS.get(res, "Unknown")
            status["resolution_code"] = res.value if res else 0

        # 4. Supported Codecs
        resp = self.send_command(HostCmd.GET_SUPPORTED_CODEC)
        if resp:
            status["supported_codecs"] = decode_supported_codecs(resp[1])

        # 5. Codec in Use
        resp = self.send_command(HostCmd.GET_CODEC_IN_USE)
        if resp:
            in_use = decode_codec_in_use(resp[1])
            status["codec_in_use"] = in_use["name"]
            status["codec_in_use_bit"] = in_use["bit"]
            status["codec_in_use_mask"] = in_use["mask"]

        # 6. LE Audio State
        resp = self.send_command(HostCmd.GET_LE_AUDIO_STATE)
        if resp:
            le_state = decode_le_audio_state(resp[1])
            status["le_audio_state"] = le_state.name
            status["le_audio_state_code"] = le_state.value

        # 7. Auracast Broadcast Info
        resp = self.send_command(HostCmd.GET_BROADCAST_INFO)
        if resp:
            bcast = decode_broadcast_info(resp[1])
            status["broadcast_enabled"] = (bcast["state"] == BroadcastState.ON_PUBLIC)
            status["broadcast_state"] = bcast["state"].name
            status["broadcast_quality"] = QUALITY_LABELS.get(bcast["quality"], bcast["quality"].name)
            status["broadcast_quality_code"] = bcast["quality"].value
            status["broadcast_encrypted"] = (bcast["encryption"] == BroadcastEncryption.ON)

        # 8. Broadcast Name
        resp = self.send_command(HostCmd.GET_BROADCAST_NAME)
        if resp:
            status["broadcast_name"] = resp[1].decode("utf-8", errors="replace").strip("\x00")

        # 9. Broadcast Encryption Key / Password
        resp = self.send_command(HostCmd.GET_BROADCAST_ENCRYPT_KEY)
        if resp:
            status["broadcast_key"] = resp[1].decode("ascii", errors="replace").strip("\x00")

        # 10. Supported Sink Transport
        resp = self.send_command(HostCmd.GET_SINK_SUPPORT_TRANSPORT)
        if resp:
            sink = decode_sink_support_transport(resp[1])
            status["sink_transport_mode"] = sink.name

        self._last_state = status
        return status

    def set_audio_mode(self, mode: AudioMode, transport: Optional[TransportMode] = None) -> bool:
        """
        Switch audio mode (0=One-to-One / High Quality, 1=Gaming, 2=Broadcast).
        """
        if not self.is_btd700:
            return False
        
        current_transport = 1  # Default BT Classic
        if transport is None:
            if "transport_mode_code" in self._last_state:
                current_transport = self._last_state["transport_mode_code"]
        else:
            current_transport = int(transport)

        args = bytes([int(mode), current_transport])
        resp = self.send_command(HostCmd.SET_AUDIO_MODE_AND_TRANSPORT, args=args)
        return resp is not None

    def set_codec(self, codec_bit_idx: int) -> bool:
        """
        Set active codec by bit index (0=SBC, 1=aptX Classic, 2=aptX Adaptive, 3=aptX Lossless, 4=aptX Lite, 5=LC3).
        """
        if not self.is_btd700:
            return False
        mask = 1 << codec_bit_idx
        resp = self.send_command(HostCmd.SET_CODEC_TO_USE, args=bytes([mask]))
        return resp is not None

    def set_broadcast_config(
        self,
        enable: Optional[bool] = None,
        name: Optional[str] = None,
        quality: Optional[BroadcastQuality] = None,
        key: Optional[str] = None
    ) -> bool:
        """Configure Auracast broadcast parameters."""
        if not self.is_btd700:
            return False

        # Set Name
        if name is not None:
            name_bytes = name.encode("utf-8")
            self.send_command(HostCmd.SET_BROADCAST_NAME, args=name_bytes)

        # Set Key
        if key is not None:
            key_bytes = key.encode("ascii")
            self.send_command(HostCmd.SET_BROADCAST_ENCRYPT_KEY, args=key_bytes)

        # Set Broadcast Info (State, Quality, Encryption)
        curr_bcast = self.send_command(HostCmd.GET_BROADCAST_INFO)
        state_val = 0
        qual_val = 2
        enc_val = 0
        if curr_bcast:
            bcast_dec = decode_broadcast_info(curr_bcast[1])
            state_val = int(bcast_dec["state"])
            qual_val = int(bcast_dec["quality"])
            enc_val = int(bcast_dec["encryption"])

        if enable is not None:
            state_val = 1 if enable else 0
        if quality is not None:
            qual_val = int(quality)
        if key is not None:
            enc_val = 1 if len(key) > 0 else 0

        info_bytes = bytes([state_val, qual_val, enc_val])
        resp = self.send_command(HostCmd.SET_BROADCAST_INFO, args=info_bytes)
        return resp is not None

    def trigger_pairing(self) -> bool:
        """Trigger Bluetooth connect / pairing mode."""
        resp = self.send_command(HostCmd.SET_BT_CONNECT, args=b'\x01')
        return resp is not None

    def factory_reset(self) -> bool:
        """Perform a factory reset on the dongle."""
        resp = self.send_command(HostCmd.SET_FACTORY_RESET, args=None)
        return resp is not None


def get_first_dongle() -> Optional[BTDDevice]:
    """Find and return the first connected Sennheiser BTD dongle."""
    devs = find_sennheiser_hid_devices()
    if not devs:
        return None
    return BTDDevice(devs[0])
