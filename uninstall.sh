#!/usr/bin/env bash
set -e

PREFIX="${PREFIX:-/usr/local}"
BIN_DIR="${PREFIX}/bin"
ICON_DIR="${PREFIX}/share/icons/hicolor/256x256/apps"
DESKTOP_DIR="${PREFIX}/share/applications"

echo "Uninstalling Sennheiser Dongle Control (btd700)..."

rm -f "${BIN_DIR}/btd700"
rm -f "${ICON_DIR}/sennheiser-btd.png"
rm -f "${DESKTOP_DIR}/sennheiser-dongle-control.desktop"

if [ "$EUID" -eq 0 ]; then
    rm -f /etc/udev/rules.d/99-sennheiser-btd.rules
    udevadm control --reload-rules || true
elif command -v sudo >/dev/null 2>&1; then
    sudo rm -f /etc/udev/rules.d/99-sennheiser-btd.rules || true
    sudo udevadm control --reload-rules || true
fi

echo "Uninstallation complete."
