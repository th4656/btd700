# Sennheiser Dongle Control for Linux (`btd700`)

A high-performance native Linux port of **Sennheiser Dongle Control** written in **Rust** with a native **Qt6 GUI** (supporting **Sennheiser BTD 700** and **Sennheiser BTD 600** Bluetooth USB adapters, as well as **HDB 630 / Momentum 4** Active Noise Cancellation control).

Provides both a fast **Command Line Interface (CLI)** and a sleek, native **Qt6 Desktop GUI**.

---

## Features

- **High-Performance Native Rust Core**:
  - Memory-safe, zero Python runtime dependencies.
  - Direct communication with `/dev/hidraw` devices using Linux ioctls.
  - Bluetooth Multipoint control using native Linux RFCOMM sockets (`AF_BLUETOOTH`).
- **Native Qt6 Desktop GUI**:
  - Full desktop GUI styled with Sennheiser's sleek dark theme aesthetic.
  - Interactive Audio Link Mode cards.
  - Real-time live stream indicators (Codec, Sample Rate, Bit Depth, Transport).
  - Clickable codec switching badges.
  - Auracast™ configuration panel (Name, Audio Quality, Password encryption).
  - Dedicated Headphone Controls card for HDB 630 / Momentum 4.
  - Background live polling with Qt timers.
- **Audio Link Modes**:
  - 🎧 **One-to-One**: High quality stereo audio up to 24-bit / 96kHz.
  - 🎮 **Gaming Mode**: Low-latency mode for gaming and voice communication.
  - 📡 **Auracast™ Broadcast**: Share audio with unlimited nearby Auracast receivers.
- **Codec Management**:
  - View current active codec in real-time.
  - Switch preferred codec (SBC, aptX Classic, aptX Adaptive, aptX Lossless, aptX Lite/QMAP, LC3).
- **Audio Quality Indicators**: Real-time sample rate (44.1 kHz, 48 kHz, 96 kHz) and bit resolution (16-bit, 24-bit).
- **Auracast Broadcast Settings**:
  - Custom broadcast name.
  - Broadcast quality (Standard 16k, Standard 24k, High Quality 48k).
  - PIN / Password encryption (public vs encrypted streams).
- **Headphone ANC & DSP Controls (HDB 630, Momentum 4, Accentum)**:
  - Toggle or set **Active Noise Cancellation (ANC)**.
  - **ANC Strength Slider** (0% - 100%).
  - **Transparency Mode Slider** (0% - 100%, dynamically synced with ANC Strength).
  - **Adaptive ANC Mode** (automatically adapts cancellation to room noise).
  - **Anti-Wind Noise Reduction** (`off`, `auto`, `max`).
  - **Bass Boost** hardware EQ.
  - Communicates directly with the headphones via Bluetooth Multipoint and **Qualcomm GAIA V3** over RFCOMM (Vendor ID `0x0495`, exactly like the Sennheiser Smart Control mobile app).
- **Dongle Operations**:
  - Trigger Bluetooth pairing / reconnect.
  - Factory reset adapter.
- **Firmware Updates (DFU)**:
  - Query Sennheiser's live firmware cloud API (`api.s-consumer-cloud.com`) for latest releases and release notes.
  - Download official `.bin` firmware packages.
  - Parse Qualcomm APPUHDR5 images.

---

## Building & Installation

### Requirements
- Rust toolchain (`cargo`, `rustc` 1.80+)
- Qt6 Development Libraries (`qt6-base` / `pkg-config` for `Qt6Widgets`, `Qt6Core`, `Qt6Gui`)
- C++17 compiler (`g++` or `clang++`)

On Arch Linux:
```bash
sudo pacman -S rust qt6-base pkg-config gcc
```

On Ubuntu / Debian:
```bash
sudo apt install cargo rustc qt6-base-dev libqt6widgets6 pkg-config g++
```

On Fedora:
```bash
sudo dnf install cargo rust qt6-qtbase-devel pkgconfig gcc-c++
```

### Build from Source
```bash
cargo build --release
```
The compiled binary will be at `target/release/btd700`.

### System-Wide Installation
To install `btd700` system-wide into `/usr/local/bin`, install udev rules, and register the desktop application menu launcher:

```bash
sudo ./install.sh
```

To uninstall:
```bash
sudo ./uninstall.sh
```

---

## Quick Start

### 1. Configure udev Rules (Non-root USB Access)
If not already installed via `install.sh`, copy the included udev rule to allow standard users to access the dongle without `sudo`:

```bash
sudo cp 99-sennheiser-btd.rules /etc/udev/rules.d/
sudo udevadm control --reload-rules && sudo udevadm trigger
```

### 2. Launch the Native Qt6 GUI
Run without arguments or pass `gui`:

```bash
btd700 gui
# Or run directly from target:
./target/release/btd700 gui
```

### 3. CLI Usage

#### List Connected Dongles
```bash
btd700 list
```

#### Query Detailed Status
```bash
btd700 status
```
Output:
```text
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Sennheiser BTD 700 Status
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Connection:       Connected
  Hardware ID:      0x3542:0x3001
  Dongle State:     Streaming Audio
  Audio Mode:       High Quality (One-to-One)
  Transport:        Classic (BR/EDR)
  Active Codec:     aptX Lossless
  Audio Quality:    96.0 kHz / 24-bit
  Supported Codecs: SBC, aptX Classic, aptX Adaptive, aptX Lossless, LC3

  [Auracast Broadcast]
    State:          OFF (Private)
    Name:           Sennheiser BTD 700
    Quality:        High Quality (48 kHz)
    Encryption:     Open
```

#### Switch Audio Link Mode
```bash
# High-Quality One-to-One
btd700 mode one-to-one

# Low Latency Gaming Mode
btd700 mode gaming

# Auracast Broadcast Mode
btd700 mode broadcast
```

#### Select Preferred Codec
```bash
# View active codec
btd700 codec

# Switch codec
btd700 codec aptx-adaptive
btd700 codec aptx-lossless
btd700 codec lc3
btd700 codec sbc
```

#### Configure Auracast Broadcast
```bash
# Turn broadcast ON with custom name and High Quality
btd700 broadcast --enable --name "Living Room TV" --quality hq

# Protect with password/PIN
btd700 broadcast --name "Private Stream" --key "1234"

# Turn broadcast OFF
btd700 broadcast --disable
```

#### Headphone ANC & DSP Controls (HDB 630 / Momentum 4)
Sennheiser headphones support **Bluetooth Multipoint**. Pair your headphones with Linux Bluetooth once. `btd700` will auto-detect your paired headphones and communicate using Qualcomm GAIA V3:

```bash
# Toggle Active Noise Cancellation (ANC)
btd700 anc toggle

# Set ANC explicitly ON or OFF
btd700 anc on
btd700 anc off

# Check ANC status
btd700 anc

# Dial ANC Strength directly (0% - 100%)
# (100% = Maximum ANC, 0% = Full Transparency)
btd700 anc-strength 100
btd700 anc-strength 75
btd700 anc-strength

# Adaptive ANC (Auto adjusts cancellation to room noise)
btd700 adaptive-anc on
btd700 adaptive-anc off
btd700 adaptive-anc

# Anti-Wind Noise Reduction (off, auto, max)
btd700 anti-wind auto
btd700 anti-wind max
btd700 anti-wind off

# Transparency mode (0% - 100%)
btd700 transparency 80
btd700 transparency

# Toggle Bass Boost hardware EQ
btd700 bass-boost on
btd700 bass-boost off
btd700 bass-boost

# Full headset status summary (ANC, Strength, Adaptive, Wind, EQ)
btd700 headset

# List paired Bluetooth devices
btd700 headsets
```

#### Pairing & Factory Reset
```bash
# Trigger pairing mode on dongle
btd700 pair

# Factory reset dongle
btd700 reset
```

#### Firmware Updates (DFU)
```bash
# Check cloud for updates and read release notes
btd700 dfu check

# Download and validate latest firmware update
btd700 dfu update

# Inspect a local .bin firmware file
btd700 dfu update --file /path/to/firmware.bin
```

---

## Technical Details

- **USB Identifiers**:
  - Vendor ID: `0x3542` (Sennheiser / Sonova)
  - BTD 600 Product ID: `0x3000` (SKU `700248`, Qualcomm QCC515x)
  - BTD 700 Product ID: `0x3001` (SKU `700434`, Qualcomm QCC518x)
- **Control Framing (Report ID 52 / `0x34`)**:
  - Host Command: `[52, 0xFE, <cmd_id>, <arg_len>, ...<args>]`
  - Dongle Response: `[52, 0xFD, <cmd_id>, <payload_len>, ...<payload>]`
- **Headphone Control Protocol (Qualcomm GAIA V3 over Bluetooth RFCOMM)**:
  - Transport: Bluetooth RFCOMM (native Linux `AF_BLUETOOTH`)
  - Magic Header: `0xFF 0x03`
  - Vendor ID: `0x0495` (Sennheiser)
  - ANC Toggle / Status: Commands `0x1A04` (Set) and `0x1A05` (Get)
  - Adaptive ANC: Commands `0x1A06` (Set) and `0x1A07` (Get)
  - Anti-Wind Reduction: Commands `0x1A08` (Set) and `0x1A09` (Get)
  - Transparency Mode: Commands `0x1A02` (Set) and `0x1A03` (Get)
  - Bass Boost EQ: Commands `0x1008` (Set) and `0x1009` (Get)
