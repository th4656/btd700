# Architecture Overview: `btd700`

`btd700` is a native Linux application designed for controlling Sennheiser USB Bluetooth adapters (BTD 600 and BTD 700) and paired Sennheiser headphones (such as HDB 630, Momentum 4, and Accentum).

It is written in **Rust** for the systems core, device drivers, and CLI, coupled with a native **Qt6 (C++)** desktop graphical interface.

---

## 1. High-Level System Architecture

```text
┌─────────────────────────────────────────────────────────────┐
│                       User Interface                        │
│   ┌───────────────────────────────┐  ┌──────────────────┐   │
│   │        Native Qt6 GUI         │  │     CLI Tool     │   │
│   │ (C++17 / Qt6Widgets / Styles) │  │  (Clap / Rust)   │   │
│   └───────────────┬───────────────┘  └────────┬─────────┘   │
└───────────────────┼───────────────────────────┼─────────────┘
                    │ C ABI (src/ffi.rs)        │
┌───────────────────▼───────────────────────────▼─────────────┐
│                    Rust Core Engine                         │
│  ┌──────────────────────────────┐  ┌──────────────────────┐ │
│  │   Dongle Driver (device.rs)  │  │ Headset (headset.rs) │ │
│  │  - Audio Link Modes          │  │ - Qualcomm GAIA V3   │ │
│  │  - Codec Management          │  │ - ANC & Transparency │ │
│  │  - Auracast Broadcast        │  │ - Adaptive ANC / Wind│ │
│  │  - Pairing & Factory Reset   │  │ - Bass Boost EQ      │ │
│  └──────────────┬───────────────┘  └──────────┬───────────┘ │
│  ┌──────────────┴───────────────┐  ┌──────────┴───────────┐ │
│  │   Linux HID (hid.rs / ioctl) │  │ Native Linux RFCOMM  │ │
│  │   /dev/hidraw via sysfs      │  │ AF_BLUETOOTH socket  │ │
│  └──────────────┬───────────────┘  └──────────┬───────────┘ │
│  ┌──────────────┴───────────────┐  ┌──────────┴───────────┐ │
│  │   Firmware DFU (dfu.rs)      │  │ Cloud (cloud.rs)     │ │
│  │   Qualcomm APPUHDR5 parser   │  │ Sennheiser Cloud API │ │
│  └──────────────────────────────┘  └──────────────────────┘ │
└───────────────────┬───────────────────────────┬─────────────┘
                    │ Linux Kernel              │ Bluetooth
┌───────────────────▼───────────────────────────▼─────────────┐
│                     Physical Hardware                       │
│  ┌──────────────────────────────┐  ┌──────────────────────┐ │
│  │ Sennheiser BTD 600 / 700 USB │  │ Sennheiser Headset   │ │
│  │ (VID 0x3542, PID 0x3000/3001)│  │ (HDB 630, M4, etc.)  │ │
│  └──────────────────────────────┘  └──────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Core Modules

### 2.1 Dongle Communication (`src/hid.rs`, `src/device.rs`, `src/protocol.rs`)
- **Discovery**: Scans `/sys/class/hidraw` to locate devices matching Sennheiser's Vendor ID (`0x3542`) and Product IDs (`0x3000` for BTD 600, `0x3001` for BTD 700).
- **Transport**: Communicates with the dongle's HID feature endpoint using Linux `HIDIOCSFEATURE` and `HIDIOCGFEATURE` ioctl calls on `/dev/hidrawX`.
- **Packet Framing**: Implements Sennheiser's proprietary Report ID 52 framing:
  - Command requests: `[52, 0xFE, cmd_id, args_len, ...args]`
  - Dongle responses: `[52, 0xFD, cmd_id, payload_len, ...payload]`

### 2.2 Headphone ANC & DSP Control (`src/headset.rs`)
- **Direct RFCOMM Communication**: Utilizes native Linux Bluetooth stream sockets (`AF_BLUETOOTH`, protocol `BTPROTO_RFCOMM=3`). No external daemon or DBus dependencies are required at runtime.
- **Auto-Discovery**: Pairs and inspects known Bluetooth devices using `bluetoothctl --timeout 2 devices`, filtering specifically for Sennheiser headphone models.
- **Protocol**: Speaks Qualcomm GAIA V3 (Vendor ID `0x0495`), the exact proprietary control protocol implemented by Sennheiser's Smart Control mobile app:
  - Framing: `[0xFF, 0x03, payload_len_be16, 0x04, 0x95, command_id_be16, ...payload]`
  - Controls ANC on/off, ANC strength (0-100%), transparency level (0-100%), adaptive ANC mode, anti-wind noise modes, and hardware bass boost.

### 2.3 Firmware Update Engine (`src/dfu.rs`, `src/cloud.rs`)
- **Cloud Client**: Communicates via HTTPS (`ureq`) with the official Sennheiser consumer cloud API (`api.s-consumer-cloud.com`) to query available updates and download signed firmware files.
- **Image Parser**: Validates and parses Qualcomm Universal Programming Module (UPM) APPUHDR5 binary images for QCC518x/QCC515x chipsets.

### 2.4 C-ABI FFI Bridge (`src/ffi.rs`, `gui/`)
- Exposes pure C-compatible functions (`extern "C"`) from the Rust engine:
  - `btd_get_full_status_json()`: Returns a unified JSON string containing both dongle status and headphone status.
  - Command wrappers: `btd_set_audio_mode()`, `btd_set_codec()`, `btd_set_anc()`, `btd_set_anc_strength()`, `btd_set_adaptive_anc()`, `btd_set_anti_wind()`, `btd_set_bass_boost()`, etc.
  - Memory safety: Allocated strings are returned as pointers and explicitly freed via `btd_free_string()`.
- **Qt6 Frontend**:
  - Built with modern C++17 and Qt6 Widgets (`QApplication`, `QMainWindow`, `QSlider`, `QComboBox`, etc.).
  - Asynchronous background polling via non-blocking `QTimer` prevents GUI stutter during USB or Bluetooth I/O.
  - Polished dark UI matching Sennheiser Smart Control design.
