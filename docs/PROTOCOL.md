# Wire Protocol Specification: BTD 600 / 700 & Headset Control

This document provides low-level technical specifications for the two distinct communication protocols used by `btd700`:
1. **Sennheiser Dongle HID Protocol** (USB HID Feature Reports over `/dev/hidraw`)
2. **Qualcomm GAIA V3 Protocol** (Bluetooth RFCOMM over `AF_BLUETOOTH`)

---

## 1. Dongle HID Protocol (Report ID 52)

Communication with the BTD 600 / 700 adapter occurs over USB HID Feature Reports using report ID `52` (`0x34`).

### 1.1 Host Request Packet Format

Host commands are sent to the dongle via `HIDIOCSFEATURE` ioctl calls:

```text
Offset  Size (bytes)  Description
0       1             Report ID (52 / 0x34)
1       1             Host Command Magic (0xFE)
2       1             Command ID (HostCmd enum)
3       1             Arguments Length (N)
4..4+N  N             Command Arguments (Optional)
```

### 1.2 Device Response Packet Format

Responses are read from the dongle via `HIDIOCGFEATURE` ioctl calls:

```text
Offset  Size (bytes)  Description
0       1             Report ID (52 / 0x34) [When returned by kernel]
1       1             Response Magic (0xFD)
2       1             Echoed Command ID
3       1             Payload Length (L)
4..4+L  L             Payload Bytes
```

### 1.3 Command Table (`HostCmd`)

| Command ID | Name | Direction | Arguments | Response Payload | Description |
|---|---|---|---|---|---|
| `0x01` | `GetAudioModeAndTransport` | Host -> Device | None | `[mode: u8, transport: u8]` | Query current audio link mode and transport type. |
| `0x02` | `SetAudioModeAndTransport` | Host -> Device | `[mode: u8, transport: u8]` | `[status: u8]` | Switch audio mode (0=HQ, 1=Gaming, 2=Broadcast). |
| `0x03` | `GetSupportedCodec` | Host -> Device | None | `[mask: u16 (BE)]` | Bitmask of codecs supported by paired sink. |
| `0x04` | `SetCodecToUse` | Host -> Device | `[codec_bit: u8]` | `[status: u8]` | Force specific active codec (0=SBC, 1=aptX, 2=aptX Adaptive, 3=aptX Lossless, 5=LC3). |
| `0x05` | `GetCodecInUse` | Host -> Device | None | `[codec_bit: u8]` | Current streaming codec. |
| `0x06` | `GetDongleState` | Host -> Device | None | `[state: u8]` | 0=Idle, 1=Pairing, 2=Connecting, 3=Connected, 4=Streaming, 5=Call. |
| `0x07` | `GetLEAudioState` | Host -> Device | None | `[state: u8]` | LE Audio connection status. |
| `0x08` | `GetAudioQuality` | Host -> Device | None | `[freq: u8, res: u8]` | `freq`: 1=44.1k, 2=48.0k, 3=96.0k; `res`: 1=16-bit, 2=24-bit, 3=32-bit. |
| `0x09` | `GetBroadcastInfo` | Host -> Device | None | `[enabled: u8, quality: u8]` | Auracast broadcast status and audio quality. |
| `0x0A` | `SetBroadcastInfo` | Host -> Device | `[enabled: u8, quality: u8]` | `[status: u8]` | Enable or disable Auracast broadcast. |
| `0x0B` | `GetBroadcastEncryptKey` | Host -> Device | None | `[has_key: u8, key: [u8; 16]]` | Auracast encryption key. |
| `0x0C` | `SetBroadcastEncryptKey` | Host -> Device | `[key: [u8; 16]]` | `[status: u8]` | Set 16-byte Auracast stream encryption password. |
| `0x0D` | `GetBroadcastName` | Host -> Device | None | `[len: u8, name_bytes...]` | UTF-8 broadcast stream name. |
| `0x0E` | `SetBroadcastName` | Host -> Device | `[len: u8, name_bytes...]` | `[status: u8]` | Set broadcast stream name. |
| `0x12` | `GetFirmwareVersion` | Host -> Device | None | `[ver_bytes: [u8; 4]]` | Adapter firmware version. |
| `0x13` | `SetFactoryReset` | Host -> Device | `[0x01]` | `[status: u8]` | Clear pairing cache and reset dongle to factory state. |
| `0x14` | `SetBTConnect` | Host -> Device | `[0x01]` | `[status: u8]` | Trigger Bluetooth pairing mode. |

---

## 2. Headphone GAIA V3 Protocol (Qualcomm / Sennheiser)

Communication with the headphones (e.g. Sennheiser HDB 630, Momentum 4) uses **Qualcomm GAIA V3** over native Linux **RFCOMM** (`AF_BLUETOOTH`, `BTPROTO_RFCOMM=3`).

### 2.1 Packet Frame Format

Each GAIA packet is framed with a big-endian header and vendor identifier:

```text
Offset  Size (bytes)  Type        Description
0..1    2             [0xFF, 0x03] Protocol Magic / Framing Header
2..3    2 (BE)        u16         Payload Length (excluding 8-byte GAIA header)
4..5    2 (BE)        u16         Vendor ID (0x0495 for Sennheiser)
6..7    2 (BE)        u16         Command ID (Feature + Command)
8..8+N  N             [u8]        Command Payload
```

### 2.2 Sennheiser GAIA Commands

| Command ID | Name | Direction | Payload Description |
|---|---|---|---|
| `0x0007` | `REGISTER_NOTIFICATION` | Host -> Headset | `[feature_id: u16]` (Subscribe to state changes) |
| `0x1A04` | `SET_ANC_STATUS` | Host -> Headset | `[state: u8]` (0 = ANC Off, 1 = ANC On) |
| `0x1A05` | `GET_ANC_STATUS` | Host -> Headset | Query: empty -> Response: `[state: u8]` |
| `0x1A02` | `SET_TRANSPARENCY` | Host -> Headset | `[level: u8]` (0 to 100). Note: ANC strength = `100 - transparency`. |
| `0x1A03` | `GET_TRANSPARENCY` | Host -> Headset | Query: empty -> Response: `[level: u8]` |
| `0x1A06` | `SET_ADAPTIVE_ANC` | Host -> Headset | `[enabled: u8]` (0 = Manual, 1 = Adaptive) |
| `0x1A07` | `GET_ADAPTIVE_ANC` | Host -> Headset | Query: empty -> Response: `[enabled: u8]` |
| `0x1A08` | `SET_ANTI_WIND` | Host -> Headset | `[mode: u8]` (0 = Off, 1 = Auto, 2 = Max) |
| `0x1A09` | `GET_ANTI_WIND` | Host -> Headset | Query: empty -> Response: `[mode: u8]` |
| `0x1008` | `SET_BASS_BOOST` | Host -> Headset | `[enabled: u8]` (0 = Off, 1 = On) |
| `0x1009` | `GET_BASS_BOOST` | Host -> Headset | Query: empty -> Response: `[enabled: u8]` |

### 2.3 RFCOMM Channel Resolution

Sennheiser headphones run the GAIA control service on an RFCOMM channel. If not specified by the user or cache, `btd700` probes channels in order of likelihood:

```text
[2, 1, 14, 15, 12, 3, 4, 5, 6, 7, 8, 9, 10, 11]
```
Upon establishing a successful handshake, the active channel is saved to `~/.config/btd700/config.json` for fast subsequent connections.
