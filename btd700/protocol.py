"""
Protocol encoder and decoder for Sennheiser BTD 700 / BTD 600 HID communication.
"""

from typing import Optional, Tuple, Dict, Any, List
from .constants import (
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
    BTD700_MAGIC_HOST_CMD,
    BTD700_MAGIC_DONGLE_RESP,
)


def build_host_command(cmd: HostCmd, args: Optional[bytes] = None) -> bytes:
    """
    Build a BTD700 host command payload for Report ID 52 (0x34).
    Wire format: [ReportID(52), 0xFE, cmd_id, arg_len, ...args]
    """
    payload = bytearray([BTD700_MAGIC_HOST_CMD, int(cmd)])
    if args:
        payload.append(len(args))
        payload.extend(args)
    else:
        payload.append(0)
    
    # Prepend Report ID 52 for output report
    packet = bytearray([ReportID.BTD700_CONTROL])
    packet.extend(payload)
    # Pad to standard report length (64 bytes)
    if len(packet) < 64:
        packet.extend(b'\x00' * (64 - len(packet)))
    return bytes(packet)


def parse_dongle_response(data: bytes) -> Optional[Tuple[int, bytes]]:
    """
    Parse a response received from the dongle.
    Wire format:
      data[0] = Report ID (52 / 0x34)
      data[1] = 0xFD (253) (magic response marker)
      data[2] = cmd
      data[3] = payload length
      data[4..4+length] = payload
    Returns (cmd, payload) or None if invalid.
    """
    if len(data) < 4:
        return None
    
    report_id = data[0]
    if report_id != ReportID.BTD700_CONTROL:
        return None
    
    magic = data[1]
    if magic != BTD700_MAGIC_DONGLE_RESP:
        return None
    
    cmd = data[2]
    payload_len = data[3]
    payload = data[4:4 + payload_len]
    return (cmd, payload)


def decode_audio_mode_and_transport(payload: bytes) -> Dict[str, Any]:
    """
    Decode response from HostCmd.GET_AUDIO_MODE_AND_TRANSPORT or DongleCmd.SET_AUDIO_MODE_AND_TRANSPORT
    payload[0] = audioMode (0=HQ/One-to-One, 1=Gaming, 2=Broadcast)
    payload[1] = transportMode (1=BT Classic, 2=LE Audio, 3=Dual)
    payload[2] = (optional) connectedTransportMode
    """
    res = {
        "audio_mode": AudioMode(payload[0]) if payload[0] in [m.value for m in AudioMode] else payload[0],
        "transport_mode": TransportMode(payload[1]) if payload[1] in [t.value for t in TransportMode] else payload[1],
    }
    if len(payload) > 2:
        res["connected_transport"] = TransportMode(payload[2]) if payload[2] in [t.value for t in TransportMode] else payload[2]
    return res


def decode_supported_codecs(payload: bytes) -> List[Dict[str, Any]]:
    """
    Decode bitmask of supported codecs from payload[0].
    """
    mask = payload[0] if len(payload) > 0 else 0
    supported = []
    for bit in CodecBit:
        is_supported = bool(mask & (1 << bit.value))
        if is_supported:
            supported.append({
                "bit": bit.value,
                "name": CODEC_NAMES.get(bit, f"Unknown ({bit})"),
                "mask": 1 << bit.value,
            })
    return supported


def decode_codec_in_use(payload: bytes) -> Dict[str, Any]:
    """
    Decode active codec from payload[0].
    """
    mask = payload[0] if len(payload) > 0 else 0
    active_bit = None
    for bit in CodecBit:
        if mask & (1 << bit.value):
            active_bit = bit
            break
    
    return {
        "mask": mask,
        "bit": active_bit.value if active_bit is not None else None,
        "name": CODEC_NAMES.get(active_bit, "Unknown/None") if active_bit is not None else "None",
    }


def decode_dongle_state(payload: bytes) -> DongleState:
    state_val = payload[0] if len(payload) > 0 else 0
    return DongleState(state_val) if state_val in [s.value for s in DongleState] else DongleState.NONE


def decode_le_audio_state(payload: bytes) -> LEAudioState:
    state_val = payload[0] if len(payload) > 0 else 0
    return LEAudioState(state_val) if state_val in [s.value for s in LEAudioState] else LEAudioState.NONE


def decode_audio_quality(payload: bytes) -> Dict[str, Any]:
    """
    payload[0] = frequency (1=44.1, 2=48, 3=96)
    payload[1] = resolution (1=16-bit, 2=24-bit)
    """
    freq_val = payload[0] if len(payload) > 0 else 0
    res_val = payload[1] if len(payload) > 1 else 0
    
    freq = AudioFrequency(freq_val) if freq_val in [f.value for f in AudioFrequency] else None
    res = AudioResolution(res_val) if res_val in [r.value for r in AudioResolution] else None
    
    return {
        "frequency": freq,
        "frequency_label": freq.name if freq else "Unknown",
        "resolution": res,
        "resolution_label": res.name if res else "Unknown",
    }


def decode_broadcast_info(payload: bytes) -> Dict[str, Any]:
    """
    payload[0] = broadcastState (0=Off, 1=On)
    payload[1] = broadcastQuality (0=16k, 1=24k, 2=HQ)
    payload[2] = broadcastEncrypt (0=Off, 1=On)
    """
    state_val = payload[0] if len(payload) > 0 else 0
    qual_val = payload[1] if len(payload) > 1 else 0
    enc_val = payload[2] if len(payload) > 2 else 0
    
    return {
        "state": BroadcastState(state_val) if state_val in [s.value for s in BroadcastState] else BroadcastState.OFF_PRIVATE,
        "quality": BroadcastQuality(qual_val) if qual_val in [q.value for q in BroadcastQuality] else BroadcastQuality.HQ,
        "encryption": BroadcastEncryption(enc_val) if enc_val in [e.value for e in BroadcastEncryption] else BroadcastEncryption.OFF,
    }


def decode_sink_support_transport(payload: bytes) -> SinkMode:
    mode_val = payload[0] if len(payload) > 0 else 0
    return SinkMode(mode_val) if mode_val in [m.value for m in SinkMode] else SinkMode.NA
