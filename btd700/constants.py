"""
Sennheiser BTD 600 / BTD 700 Protocol Constants and Enums
Reverse-engineered from Sennheiser Dongle Control (v1.0.5)
"""

from enum import IntEnum

# USB Device Identifiers
SENNHEISER_VID = 0x3542  # 13634

BTD600_PID = 0x3000      # 12288
BTD700_PID = 0x3001      # 12289

# HID Report IDs
class ReportID(IntEnum):
    DFU_COMMAND = 3
    DFU_DATA_TRANSFER = 5
    DFU_RESPONSE = 6
    BTD700_CONTROL = 52  # 0x34

# BTD 700 Framing Markers
BTD700_MAGIC_HOST_CMD = 0xFE     # 254
BTD700_MAGIC_DONGLE_RESP = 0xFD  # 253

# BTD 700 Host Commands (sent by host to dongle)
class HostCmd(IntEnum):
    GET_AUDIO_MODE_AND_TRANSPORT = 1
    SET_AUDIO_MODE_AND_TRANSPORT = 2
    GET_SUPPORTED_CODEC = 3
    SET_CODEC_TO_USE = 4
    GET_CODEC_IN_USE = 5
    GET_DONGLE_STATE = 6
    GET_LE_AUDIO_STATE = 7
    GET_AUDIO_QUALITY = 8
    GET_BROADCAST_INFO = 9
    SET_BROADCAST_INFO = 10
    GET_BROADCAST_ENCRYPT_KEY = 11
    SET_BROADCAST_ENCRYPT_KEY = 12
    GET_BROADCAST_NAME = 13
    SET_BROADCAST_NAME = 14
    GET_FIRMWARE_VERSION = 18
    SET_FACTORY_RESET = 19
    SET_BT_CONNECT = 20
    GET_SINK_SUPPORT_TRANSPORT = 21
    GET_GAMING_AVAILABLE_STATUS = 23

# BTD 700 Dongle Commands (received in responses/notifications)
class DongleCmd(IntEnum):
    SET_AUDIO_MODE_AND_TRANSPORT = 2
    SET_SUPPORTED_CODEC = 3
    SET_CODEC_TO_USE = 4
    SET_DONGLE_STATE = 15
    SET_LE_AUDIO_STATE = 16
    SET_AUDIO_QUALITY = 17
    SET_SINK_SUPPORT_TRANSPORT = 22
    SET_GAMING_AVAILABLE_STATUS = 23

# Audio Modes
class AudioMode(IntEnum):
    HIGH_QUALITY = 0  # One-to-One
    GAMING = 1        # Low Latency Gaming
    BROADCAST = 2     # Auracast Broadcast

# Transport Modes
class TransportMode(IntEnum):
    DISCONNECTED = 0
    BR_EDR = 1        # Bluetooth Classic
    LE_AUDIO = 2      # Bluetooth LE Audio
    DUAL = 3

# Sink Modes
class SinkMode(IntEnum):
    NA = 0
    BR_EDR = 1
    LE_AUDIO = 2
    DUAL = 3

# Codec Bit Positions
class CodecBit(IntEnum):
    SBC = 0                          # Bit 0 (0x01)
    APTX_CLASSIC = 1                 # Bit 1 (0x02)
    APTX_ADAPTIVE_LOWLATENCY = 2     # Bit 2 (0x04)
    APTX_LOSSLESS = 3                # Bit 3 (0x08)
    APTX_LITE_QMAP = 4               # Bit 4 (0x10)
    LC3 = 5                          # Bit 5 (0x20)

CODEC_NAMES = {
    CodecBit.SBC: "SBC",
    CodecBit.APTX_CLASSIC: "aptX Classic",
    CodecBit.APTX_ADAPTIVE_LOWLATENCY: "aptX Adaptive",
    CodecBit.APTX_LOSSLESS: "aptX Lossless",
    CodecBit.APTX_LITE_QMAP: "aptX Lite (QMAP)",
    CodecBit.LC3: "LC3",
}

# Dongle States
class DongleState(IntEnum):
    NONE = 0
    DISCONNECTED = 1
    CONNECTED = 2
    STREAMING_AUDIO = 3
    STREAMING_VOICE = 4

# LE Audio States
class LEAudioState(IntEnum):
    NONE = 0
    DISCONNECTED = 1
    CONNECTED = 2
    STREAMING_UNICAST = 3
    STREAMING_BROADCAST = 4

# Audio Sampling Frequency
class AudioFrequency(IntEnum):
    FREQ_44_1KHZ = 1
    FREQ_48KHZ = 2
    FREQ_96KHZ = 3

FREQ_LABELS = {
    AudioFrequency.FREQ_44_1KHZ: "44.1 kHz",
    AudioFrequency.FREQ_48KHZ: "48.0 kHz",
    AudioFrequency.FREQ_96KHZ: "96.0 kHz",
}

# Audio Bit Resolution
class AudioResolution(IntEnum):
    RES_16BIT = 1
    RES_24BIT = 2

RES_LABELS = {
    AudioResolution.RES_16BIT: "16-bit",
    AudioResolution.RES_24BIT: "24-bit",
}

# Auracast Broadcast State
class BroadcastState(IntEnum):
    OFF_PRIVATE = 0
    ON_PUBLIC = 1

# Auracast Broadcast Quality
class BroadcastQuality(IntEnum):
    SQ_16K = 0  # Standard Quality 16k
    SQ_24K = 1  # Standard Quality 24k
    HQ = 2      # High Quality

QUALITY_LABELS = {
    BroadcastQuality.SQ_16K: "Standard Quality (16 kHz)",
    BroadcastQuality.SQ_24K: "Standard Quality (24 kHz)",
    BroadcastQuality.HQ: "High Quality (48 kHz)",
}

# Auracast Encryption / Password
class BroadcastEncryption(IntEnum):
    OFF = 0
    ON = 1

# Device Types
class DeviceType(IntEnum):
    DEV_BTD600 = 0
    DEV_BTD700 = 1

KNOWN_DEVICES = {
    (SENNHEISER_VID, BTD600_PID): {
        "type": DeviceType.DEV_BTD600,
        "name": "BTD 600",
        "sku": "700248",
        "chip_prefix": "QCC515Xx",
        "reports": [ReportID.DFU_COMMAND],
        "image": "btd600.png",
    },
    (SENNHEISER_VID, BTD700_PID): {
        "type": DeviceType.DEV_BTD700,
        "name": "BTD 700",
        "sku": "700434",
        "chip_prefix": "QCC518Xx",
        "reports": [ReportID.DFU_COMMAND, ReportID.BTD700_CONTROL],
        "image": "btd700.png",
    }
}

# Qualcomm UPM / DFU Constants
class UP_Op(IntEnum):
    UPGRADE_START_REQ = 1
    UPGRADE_START_CFM = 2
    UPGRADE_DATA_BYTES_REQ = 3
    UPGRADE_DATA = 4
    UPGRADE_ABORT_REQ = 7
    UPGRADE_ABORT_CFM = 8
    UPGRADE_TRANSFER_COMPLETE_IND = 11
    UPGRADE_TRANSFER_COMPLETE_RES = 12
    UPGRADE_PROCEED_TO_COMMIT = 14
    UPGRADE_COMMIT_REQ = 15
    UPGRADE_COMMIT_CFM = 16
    UPGRADE_ERROR_IND = 17
    UPGRADE_COMPLETE_IND = 18
    UPGRADE_SYNC_REQ = 19
    UPGRADE_SYNC_CFM = 20
    UPGRADE_START_DATA_REQ = 21
    UPGRADE_IS_VALIDATION_DONE_REQ = 22
    UPGRADE_IS_VALIDATION_DONE_CFM = 23
    UPGRADE_HOST_VERSION_REQ = 24
    UPGRADE_HOST_VERSION_CFM = 25
    UPGRADE_ERROR_RES = 26

class U_HIDCmd(IntEnum):
    HID_CMD_CONNECTION_REQ = 2
    HID_CMD_DISCONNECT_REQ = 7

# Cloud Firmware Repository API
API_BASE_URL = "https://api.s-consumer-cloud.com/firmware/api/v2/"
