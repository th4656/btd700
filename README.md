# Sennheiser Dongle Control for Linux (`btd700`)

A native Linux port of **Sennheiser Dongle Control** (supporting **Sennheiser BTD 700** and **Sennheiser BTD 600** Bluetooth USB adapters).

Provides both a powerful **Command Line Interface (CLI)** and a sleek **Web GUI Control Panel** with zero mandatory third-party package dependencies (uses native Linux `/dev/hidraw` ioctls and standard Python 3.8+).

---

## Features

- **Dongle Detection & Status**: Automatically detects BTD 700 (`0x3542:0x3001`) and BTD 600 (`0x3542:0x3000`).
- **Audio Modes**:
  - 🎧 **One-to-One**: High quality stereo audio up to 24-bit / 96kHz.
  - 🎮 **Gaming Mode**: Low-latency mode for interactive gaming and voice chat.
  - 📡 **Auracast™ Broadcast**: Share audio with unlimited nearby Auracast receivers.
- **Codec Management**:
  - View current active codec in real-time.
  - Switch preferred codec (SBC, aptX Classic, aptX Adaptive, aptX Lossless, aptX Lite/QMAP, LC3).
- **Audio Quality Indicators**: Real-time sample rate (44.1 kHz, 48 kHz, 96 kHz) and bit resolution (16-bit, 24-bit).
- **Auracast Broadcast Settings**:
  - Custom broadcast name.
  - Broadcast quality (Standard 16k, Standard 24k, High Quality 48k).
  - PIN / Password encryption (public vs encrypted streams).
  - Public Broadcast Profile (PBP).
- **Controls**:
  - Trigger Bluetooth pairing / reconnect.
  - Factory reset adapter.
- **Firmware Update (DFU)**:
  - Query Sennheiser's live firmware cloud API (`api.s-consumer-cloud.com`) for latest releases and release notes.
  - Download official `.bin` firmware packages.
  - Flash Qualcomm APPUHDR5 images via Qualcomm UPM (Universal Programming Module) protocol.
- **Web GUI Control Panel**:
  - Browser-based visual interface matching Sennheiser's dark theme aesthetic.
  - Official high-res product renders and branding.

---

## Quick Start

### 1. Configure udev Rules (Non-root USB Access)
Copy the included udev rule to allow standard users to communicate with the dongle without `sudo`:

```bash
sudo cp 99-sennheiser-btd.rules /etc/udev/rules.d/
sudo udevadm control --reload-rules && sudo udevadm trigger
```

### 2. Launch the Web GUI
Launch the visual control panel in your default browser:

```bash
./btd700.py gui
# Or specify a custom port:
./btd700.py gui --port 8700
```

### 3. CLI Usage

#### List Connected Devices
```bash
./btd700.py list
```

#### Query Detailed Status
```bash
./btd700.py status
```
Output:
```text
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  BTD 700 Status
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Connection:       Connected
  Hardware ID:      0x3542:0x3001
  Dongle State:     STREAMING_AUDIO
  Audio Mode:       HIGH_QUALITY
  Transport:        BR_EDR
  Active Codec:     aptX Lossless
  Audio Quality:    96.0 kHz / 24-bit
  Supported Codecs: SBC, aptX Classic, aptX Adaptive, aptX Lossless, LC3

  [Auracast Broadcast]
    State:          OFF (Private)
    Stream Name:    Sennheiser BTD 700
    Quality:        High Quality (48 kHz)
    Encryption:     Open (Unencrypted)
```

#### Switch Audio Mode
```bash
# High-Quality One-to-One
./btd700.py mode one-to-one

# Low Latency Gaming Mode
./btd700.py mode gaming

# Auracast Broadcast Mode
./btd700.py mode broadcast
```

#### Select Preferred Codec
```bash
# View active codec
./btd700.py codec

# Switch to specific codec
./btd700.py codec aptx-adaptive
./btd700.py codec aptx-lossless
./btd700.py codec lc3
./btd700.py codec sbc
```

#### Configure Auracast Broadcast
```bash
# Turn broadcast ON with custom name and High Quality
./btd700.py broadcast --enable --name "Living Room TV" --quality hq

# Protect with password/PIN
./btd700.py broadcast --name "Private Stream" --key "1234"

# Turn broadcast OFF
./btd700.py broadcast --disable
```

#### Pairing & Reset
```bash
# Trigger pairing mode
./btd700.py pair

# Factory reset
./btd700.py reset
```

#### Firmware Updates (DFU)
```bash
# Check cloud for updates and read release notes
./btd700.py dfu check

# Download and validate latest firmware update
./btd700.py dfu update

# Flash a local .bin firmware file
./btd700.py dfu update --file /path/to/firmware.bin
```

---

## System Installation

To install `btd700` system-wide into `/usr/local/bin` and create application menu shortcuts:

```bash
sudo ./install.sh
```

To uninstall:
```bash
sudo ./uninstall.sh
```

Or install via Python pip:
```bash
pip install .
```

---

## Protocol Technical Details

Re-engineered directly from Sennheiser Dongle Control (MSIL/WPF):

- **USB Identifiers**:
  - Vendor ID: `0x3542` (Sennheiser / Sonova)
  - BTD 600 Product ID: `0x3000` (SKU `700248`, Qualcomm QCC515x)
  - BTD 700 Product ID: `0x3001` (SKU `700434`, Qualcomm QCC518x)
- **Control Framing (Report ID 52 / `0x34`)**:
  - Host Command: `[52, 0xFE, <cmd_id>, <arg_len>, ...<args>]`
  - Dongle Response: `[52, 0xFD, <cmd_id>, <payload_len>, ...<payload>]`
- **Qualcomm UPM Protocol (Report IDs 3, 5, 6)**:
  - Feature Report ID 3: Connect / Disconnect request (`U_HIDCmd`)
  - Output Report ID 5: Data transfers & UPM operations (`UP_Op`)
  - Input Report ID 6: UPM responses and confirmations
- **Cloud API**:
  - Base: `https://api.s-consumer-cloud.com/firmware/api/v2/`
  - Versions: `availableSystemReleases/<SKU>?os_type=android`
  - Release manifest: `systemRelease/<SKU>/<version>?os_type=android`
  - Release notes: `getExtras/<SKU>/<version>?os_type=android`
