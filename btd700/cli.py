"""
Command-line interface (CLI) for Sennheiser BTD 600 / BTD 700 on Linux.
"""

import sys
import time
import argparse
from typing import Optional

from .constants import (
    AudioMode,
    TransportMode,
    CodecBit,
    CODEC_NAMES,
    BroadcastQuality,
    KNOWN_DEVICES,
)
from .hid import find_sennheiser_hid_devices
from .device import BTDDevice, get_first_dongle
from .cloud import SennheiserCloudClient
from .dfu import DFUImage


def format_header(title: str) -> str:
    line = "━" * 50
    return f"\033[1;36m{line}\n  {title}\n{line}\033[0m"


def cmd_list(args):
    devs = find_sennheiser_hid_devices()
    if not devs:
        print("No Sennheiser BTD dongles detected.")
        print("Make sure the dongle is plugged in and udev rules are installed.")
        return 1
    
    print(format_header("Sennheiser Dongles Detected"))
    for idx, d in enumerate(devs):
        print(f"[{idx}] {d['model_name']} ({d['product_name']})")
        print(f"    Path:         {d['dev_path']}")
        print(f"    VID/PID:      0x{d['vid']:04X}:0x{d['pid']:04X}")
        if d['serial']:
            print(f"    Serial:       {d['serial']}")
        print(f"    Report IDs:   {d['report_ids']}")
    return 0


def cmd_status(args):
    dev = get_first_dongle()
    if not dev:
        print("Error: No Sennheiser BTD dongle found.")
        return 1

    try:
        dev.open()
    except PermissionError as e:
        print(f"Permission error: {e}")
        return 1

    try:
        status = dev.get_status()
    finally:
        dev.close()

    print(format_header(f"{status['model']} Status"))
    print(f"  Connection:       \033[1;32mConnected\033[0m")
    if status.get("serial"):
        print(f"  Serial Number:    {status['serial']}")
    print(f"  Hardware ID:      {status['vid']}:{status['pid']}")

    if "dongle_state" in status:
        st_color = "\033[1;32m" if "STREAMING" in status["dongle_state"] else "\033[1;34m"
        print(f"  Dongle State:     {st_color}{status['dongle_state']}\033[0m")

    if "audio_mode" in status:
        print(f"  Audio Mode:       \033[1;35m{status['audio_mode']}\033[0m")
    if "transport_mode" in status:
        print(f"  Transport:        {status['transport_mode']}")
    if "connected_transport" in status:
        print(f"  Connected Via:    {status['connected_transport']}")

    if "codec_in_use" in status:
        print(f"  Active Codec:     \033[1;33m{status['codec_in_use']}\033[0m")
    if "frequency" in status and "resolution" in status:
        print(f"  Audio Quality:    {status['frequency']} / {status['resolution']}")

    if "supported_codecs" in status:
        codecs_str = ", ".join([c["name"] for c in status["supported_codecs"]])
        print(f"  Supported Codecs: {codecs_str}")

    if "broadcast_enabled" in status:
        bcast_st = "\033[1;32mON (Public)\033[0m" if status["broadcast_enabled"] else "OFF (Private)"
        print(f"\n  [Auracast Broadcast]")
        print(f"    State:          {bcast_st}")
        if "broadcast_name" in status:
            print(f"    Stream Name:    {status['broadcast_name']}")
        if "broadcast_quality" in status:
            print(f"    Quality:        {status['broadcast_quality']}")
        if "broadcast_encrypted" in status:
            enc_st = "Encrypted" if status["broadcast_encrypted"] else "Open (Unencrypted)"
            print(f"    Encryption:     {enc_st}")
        if status.get("broadcast_key"):
            print(f"    Password/PIN:   {status['broadcast_key']}")

    return 0


def cmd_mode(args):
    dev = get_first_dongle()
    if not dev:
        print("Error: No Sennheiser BTD dongle found.")
        return 1

    mode_map = {
        "one-to-one": AudioMode.HIGH_QUALITY,
        "hq": AudioMode.HIGH_QUALITY,
        "gaming": AudioMode.GAMING,
        "broadcast": AudioMode.BROADCAST,
        "auracast": AudioMode.BROADCAST,
    }

    target = args.mode.lower()
    if target not in mode_map:
        print(f"Unknown mode: {args.mode}. Choose from: one-to-one, gaming, broadcast")
        return 1

    mode = mode_map[target]
    dev.open()
    try:
        ok = dev.set_audio_mode(mode)
        if ok:
            print(f"Successfully switched audio mode to: \033[1;32m{mode.name}\033[0m")
            return 0
        else:
            print("Failed to set audio mode.")
            return 1
    finally:
        dev.close()


def cmd_codec(args):
    dev = get_first_dongle()
    if not dev:
        print("Error: No Sennheiser BTD dongle found.")
        return 1

    codec_map = {
        "sbc": 0,
        "aptx": 1,
        "aptx-classic": 1,
        "aptx-adaptive": 2,
        "adaptive": 2,
        "aptx-lossless": 3,
        "lossless": 3,
        "aptx-lite": 4,
        "qmap": 4,
        "lc3": 5,
    }

    dev.open()
    try:
        if args.codec is None:
            # Query codec
            status = dev.get_status()
            print(f"Active Codec: \033[1;33m{status.get('codec_in_use', 'Unknown')}\033[0m")
            return 0

        target = args.codec.lower()
        if target not in codec_map:
            print(f"Unknown codec: {args.codec}. Choose from: sbc, aptx, aptx-adaptive, aptx-lossless, aptx-lite, lc3")
            return 1

        bit_idx = codec_map[target]
        ok = dev.set_codec(bit_idx)
        if ok:
            print(f"Successfully selected codec: \033[1;32m{CODEC_NAMES.get(CodecBit(bit_idx))}\033[0m")
            return 0
        else:
            print("Failed to set codec.")
            return 1
    finally:
        dev.close()


def cmd_broadcast(args):
    dev = get_first_dongle()
    if not dev:
        print("Error: No Sennheiser BTD dongle found.")
        return 1

    qual_map = {
        "sq16": BroadcastQuality.SQ_16K,
        "sq24": BroadcastQuality.SQ_24K,
        "hq": BroadcastQuality.HQ,
    }

    enable = None
    if args.enable:
        enable = True
    elif args.disable:
        enable = False

    quality = None
    if args.quality:
        quality = qual_map.get(args.quality.lower())
        if quality is None:
            print("Quality must be one of: sq16, sq24, hq")
            return 1

    dev.open()
    try:
        ok = dev.set_broadcast_config(
            enable=enable,
            name=args.name,
            quality=quality,
            key=args.key
        )
        if ok:
            print("Auracast broadcast configuration updated successfully.")
            return 0
        else:
            print("Failed to update broadcast configuration.")
            return 1
    finally:
        dev.close()


def cmd_pair(args):
    dev = get_first_dongle()
    if not dev:
        print("Error: No Sennheiser BTD dongle found.")
        return 1
    dev.open()
    try:
        ok = dev.trigger_pairing()
        if ok:
            print("Triggered Bluetooth connection / pairing mode.")
            return 0
        else:
            print("Failed to trigger pairing mode.")
            return 1
    finally:
        dev.close()


def cmd_reset(args):
    dev = get_first_dongle()
    if not dev:
        print("Error: No Sennheiser BTD dongle found.")
        return 1
    dev.open()
    try:
        ok = dev.factory_reset()
        if ok:
            print("Factory reset command sent to dongle.")
            return 0
        else:
            print("Failed to send factory reset command.")
            return 1
    finally:
        dev.close()


def cmd_dfu(args):
    client = SennheiserCloudClient()
    dev = get_first_dongle()
    sku = "700434"  # Default BTD 700
    model = "BTD 700"
    if dev:
        info = dev.dev_info.get("device_info", {})
        sku = info.get("sku", sku)
        model = info.get("name", model)

    if args.action == "check":
        print(format_header(f"Checking Firmware Updates for {model} (SKU: {sku})"))
        try:
            versions = client.get_available_versions(sku)
            if not versions:
                print("No firmware versions available.")
                return 0
            latest = versions[0]
            print(f"  Latest Available Version: \033[1;32m{latest}\033[0m")
            print(f"  All Versions:             {', '.join(versions)}")

            notes = client.get_release_notes(sku, latest)
            if notes:
                print("\n  \033[1mRelease Notes:\033[0m")
                for line in notes.strip().split("\n"):
                    print(f"    {line}")

            dl_url = client.get_dfu_download_url(sku, latest)
            if dl_url:
                print(f"\n  Download URL: {dl_url}")
            return 0
        except Exception as e:
            print(f"Error checking cloud updates: {e}")
            return 1

    elif args.action == "update":
        print(format_header("Firmware Update (DFU)"))
        if args.file:
            img = DFUImage(args.file)
            if not img.is_valid:
                print(f"Error parsing DFU image: {img.error}")
                return 1
            print(f"Local DFU file loaded: {args.file}")
            print(f"  Firmware Version: {img.firmware_version}")
            print(f"  Chip Model:       {img.chip_model}")
            print(f"  Partitions:       {len(img.partitions)}")
            print("\nReady to flash firmware to dongle.")
            return 0
        else:
            versions = client.get_available_versions(sku)
            if not versions:
                print("No firmware versions found.")
                return 1
            latest = versions[0]
            dl_url = client.get_dfu_download_url(sku, latest)
            print(f"Downloading latest firmware ({latest})...")
            dest = f"/tmp/btd_dfu_{latest}.bin"
            client.download_firmware(dl_url, dest, lambda cur, tot: print(f"  Progress: {cur}/{tot} bytes ({(cur/tot)*100:.1f}%)", end="\r"))
            print(f"\nSaved firmware to {dest}")
            img = DFUImage(dest)
            print(f"Validated DFU image: version {img.firmware_version}, chip {img.chip_model}")
            return 0


def cmd_web(args):
    from .web import run_server
    port = args.port or 8700
    run_server(port=port, open_browser=not args.no_browser)
    return 0


def main():
    parser = argparse.ArgumentParser(
        prog="btd700",
        description="Sennheiser Dongle Control (BTD 600 / BTD 700) for Linux"
    )
    sub = parser.add_subparsers(dest="command", required=True)

    # list
    sub.add_parser("list", help="List connected Sennheiser BTD dongles")

    # status
    sub.add_parser("status", help="Show full dongle state, codec, mode, and broadcast info")

    # mode
    p_mode = sub.add_parser("mode", help="Switch audio mode (one-to-one, gaming, broadcast)")
    p_mode.add_argument("mode", choices=["one-to-one", "hq", "gaming", "broadcast", "auracast"])

    # codec
    p_codec = sub.add_parser("codec", help="View or set audio codec")
    p_codec.add_argument("codec", nargs="?", choices=["sbc", "aptx", "aptx-adaptive", "aptx-lossless", "aptx-lite", "lc3"])

    # broadcast
    p_bcast = sub.add_parser("broadcast", help="Configure Auracast broadcast")
    p_bcast.add_argument("--enable", action="store_true", help="Turn broadcast ON")
    p_bcast.add_argument("--disable", action="store_true", help="Turn broadcast OFF")
    p_bcast.add_argument("--name", type=str, help="Broadcast name")
    p_bcast.add_argument("--quality", choices=["sq16", "sq24", "hq"], help="Broadcast quality")
    p_bcast.add_argument("--key", type=str, help="Broadcast encryption PIN / password")

    # pair
    sub.add_parser("pair", help="Trigger Bluetooth pairing mode")

    # reset
    sub.add_parser("reset", help="Factory reset dongle")

    # dfu
    p_dfu = sub.add_parser("dfu", help="Firmware update management")
    p_dfu.add_argument("action", choices=["check", "update"])
    p_dfu.add_argument("--file", type=str, help="Local .bin firmware file")

    # web / gui
    p_web = sub.add_parser("web", aliases=["gui"], help="Launch the Web GUI Control Panel")
    p_web.add_argument("--port", type=int, default=8700, help="Web server port (default 8700)")
    p_web.add_argument("--no-browser", action="store_true", help="Do not automatically launch web browser")

    parsed = parser.parse_args()

    handlers = {
        "list": cmd_list,
        "status": cmd_status,
        "mode": cmd_mode,
        "codec": cmd_codec,
        "broadcast": cmd_broadcast,
        "pair": cmd_pair,
        "reset": cmd_reset,
        "dfu": cmd_dfu,
        "web": cmd_web,
        "gui": cmd_web,
    }

    fn = handlers.get(parsed.command)
    if fn:
        sys.exit(fn(parsed) or 0)


if __name__ == "__main__":
    main()
