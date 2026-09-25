#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PREFIX="${PREFIX:-/usr/local}"
BIN_DIR="${PREFIX}/bin"
ICON_DIR="${PREFIX}/share/icons/hicolor/256x256/apps"
DESKTOP_DIR="${PREFIX}/share/applications"

echo "Installing Sennheiser Dongle Control (btd700)..."

# Install udev rule if root or sudo available
if [ "$EUID" -eq 0 ]; then
    echo "Installing udev rules to /etc/udev/rules.d/..."
    cp "${SCRIPT_DIR}/99-sennheiser-btd.rules" /etc/udev/rules.d/
    udevadm control --reload-rules && udevadm trigger || true
elif command -v sudo >/dev/null 2>&1; then
    echo "Prompting for sudo to install udev rules to /etc/udev/rules.d/..."
    sudo cp "${SCRIPT_DIR}/99-sennheiser-btd.rules" /etc/udev/rules.d/
    sudo udevadm control --reload-rules && sudo udevadm trigger || true
else
    echo "Note: Could not install udev rules automatically without sudo/root."
    echo "To allow non-root access, run: sudo cp ${SCRIPT_DIR}/99-sennheiser-btd.rules /etc/udev/rules.d/"
fi

# Install btd700 executable
mkdir -p "${BIN_DIR}"
ln -sf "${SCRIPT_DIR}/btd700.py" "${BIN_DIR}/btd700"
chmod +x "${BIN_DIR}/btd700"

# Install icons and desktop launcher
mkdir -p "${ICON_DIR}" "${DESKTOP_DIR}"
cp "${SCRIPT_DIR}/btd700/assets/app_icon.png" "${ICON_DIR}/sennheiser-btd.png" 2>/dev/null || true
cp "${SCRIPT_DIR}/sennheiser-dongle-control.desktop" "${DESKTOP_DIR}/" 2>/dev/null || true

echo "Installation complete!"
echo "You can now run 'btd700' from your terminal or launch 'Sennheiser Dongle Control' from your app menu."
