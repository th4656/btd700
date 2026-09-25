//! Constants and protocol definitions for Sennheiser BTD 600 / BTD 700 & Headset control.

use serde::{Deserialize, Serialize};

pub const VID_SENNHEISER: u16 = 0x3542;
pub const PID_BTD600: u16 = 0x3000;
pub const PID_BTD700: u16 = 0x3001;

pub const BTD700_REPORT_ID: u8 = 52;
pub const DFU_CMD_REPORT_ID: u8 = 3;
pub const DFU_DATA_REPORT_ID: u8 = 5;
pub const DFU_RESP_REPORT_ID: u8 = 6;

// Qualcomm GAIA Protocol Constants for Sennheiser
pub const GAIA_MAGIC: [u8; 2] = [0xFF, 0x03];
pub const VENDOR_SENNHEISER: u16 = 0x0495;

pub const GAIA_CMD_REGISTER_NOTIFICATION: u16 = 0x0007;
pub const GAIA_CMD_SET_BASS_BOOST: u16 = 0x1008;
pub const GAIA_CMD_GET_BASS_BOOST: u16 = 0x1009;
pub const GAIA_CMD_SET_TRANSPARENCY: u16 = 0x1A02;
pub const GAIA_CMD_GET_TRANSPARENCY: u16 = 0x1A03;
pub const GAIA_CMD_SET_ANC_STATUS: u16 = 0x1A04;
pub const GAIA_CMD_GET_ANC_STATUS: u16 = 0x1A05;
pub const GAIA_CMD_SET_ADAPTIVE_ANC: u16 = 0x1A06;
pub const GAIA_CMD_GET_ADAPTIVE_ANC: u16 = 0x1A07;
pub const GAIA_CMD_SET_ANTI_WIND: u16 = 0x1A08;
pub const GAIA_CMD_GET_ANTI_WIND: u16 = 0x1A09;

pub const COMMON_RFCOMM_CHANNELS: [u8; 14] = [2, 1, 14, 15, 12, 3, 4, 5, 6, 7, 8, 9, 10, 11];

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[repr(u8)]
pub enum AudioMode {
    HighQuality = 0,
    Gaming = 1,
    Broadcast = 2,
}

impl AudioMode {
    pub fn from_u8(v: u8) -> Self {
        match v {
            1 => AudioMode::Gaming,
            2 => AudioMode::Broadcast,
            _ => AudioMode::HighQuality,
        }
    }

    pub fn as_str(&self) -> &'static str {
        match self {
            AudioMode::HighQuality => "High Quality (One-to-One)",
            AudioMode::Gaming => "Gaming Mode",
            AudioMode::Broadcast => "Auracast Broadcast",
        }
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[repr(u8)]
pub enum TransportMode {
    None = 0,
    Bredr = 1,
    LeAudio = 2,
}

impl TransportMode {
    pub fn from_u8(v: u8) -> Self {
        match v {
            1 => TransportMode::Bredr,
            2 => TransportMode::LeAudio,
            _ => TransportMode::None,
        }
    }

    pub fn as_str(&self) -> &'static str {
        match self {
            TransportMode::Bredr => "Classic (BR/EDR)",
            TransportMode::LeAudio => "LE Audio",
            TransportMode::None => "None / Disconnected",
        }
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[repr(u8)]
pub enum CodecBit {
    Sbc = 0,
    Aptx = 1,
    AptxAdaptive = 2,
    AptxLossless = 3,
    AptxLite = 4,
    Lc3 = 5,
}

impl CodecBit {
    pub fn from_bit(b: u8) -> Option<Self> {
        match b {
            0 => Some(CodecBit::Sbc),
            1 => Some(CodecBit::Aptx),
            2 => Some(CodecBit::AptxAdaptive),
            3 => Some(CodecBit::AptxLossless),
            4 => Some(CodecBit::AptxLite),
            5 => Some(CodecBit::Lc3),
            _ => None,
        }
    }

    pub fn name(&self) -> &'static str {
        match self {
            CodecBit::Sbc => "SBC",
            CodecBit::Aptx => "aptX Classic",
            CodecBit::AptxAdaptive => "aptX Adaptive",
            CodecBit::AptxLossless => "aptX Lossless",
            CodecBit::AptxLite => "aptX Lite / QMAP",
            CodecBit::Lc3 => "LC3",
        }
    }

    pub fn from_name(name: &str) -> Option<Self> {
        let n = name.to_lowercase().replace(['-', '_', ' '], "");
        match n.as_str() {
            "sbc" => Some(CodecBit::Sbc),
            "aptx" | "aptxclassic" => Some(CodecBit::Aptx),
            "aptxadaptive" | "adaptive" => Some(CodecBit::AptxAdaptive),
            "aptxlossless" | "lossless" => Some(CodecBit::AptxLossless),
            "aptxlite" | "qmap" => Some(CodecBit::AptxLite),
            "lc3" => Some(CodecBit::Lc3),
            _ => None,
        }
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[repr(u8)]
pub enum BroadcastQuality {
    Sq16 = 0,
    Sq24 = 1,
    Hq = 2,
}

impl BroadcastQuality {
    pub fn from_u8(v: u8) -> Self {
        match v {
            0 => BroadcastQuality::Sq16,
            1 => BroadcastQuality::Sq24,
            _ => BroadcastQuality::Hq,
        }
    }

    pub fn as_str(&self) -> &'static str {
        match self {
            BroadcastQuality::Sq16 => "Standard Quality (16 kHz)",
            BroadcastQuality::Sq24 => "Standard Quality (24 kHz)",
            BroadcastQuality::Hq => "High Quality (48 kHz)",
        }
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
#[repr(u8)]
pub enum HostCmd {
    GetAudioModeAndTransport = 1,
    SetAudioModeAndTransport = 2,
    GetSupportedCodec = 3,
    SetCodecToUse = 4,
    GetCodecInUse = 5,
    GetDongleState = 6,
    GetLEAudioState = 7,
    GetAudioQuality = 8,
    GetBroadcastInfo = 9,
    SetBroadcastInfo = 10,
    GetBroadcastEncryptKey = 11,
    SetBroadcastEncryptKey = 12,
    GetBroadcastName = 13,
    SetBroadcastName = 14,
    GetFirmwareVersion = 18,
    SetFactoryReset = 19,
    SetBTConnect = 20,
    GetSinkSupportTransport = 21,
    GetGamingAvailableStatus = 23,
}

pub fn audio_resolution_str(res: u8) -> &'static str {
    match res {
        1 => "16-bit",
        2 => "24-bit",
        3 => "32-bit",
        _ => "Unknown bit depth",
    }
}

pub fn audio_frequency_str(freq: u8) -> &'static str {
    match freq {
        1 => "44.1 kHz",
        2 => "48.0 kHz",
        3 => "96.0 kHz",
        _ => "Unknown sample rate",
    }
}

pub fn dongle_state_str(st: u8) -> &'static str {
    match st {
        0 => "Idle / Standby",
        1 => "Pairing Mode",
        2 => "Connecting",
        3 => "Connected",
        4 => "Streaming Audio",
        5 => "Voice Call Active",
        _ => "Unknown State",
    }
}
