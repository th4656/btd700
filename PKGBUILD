# Maintainer: th4656
pkgname=btd700
pkgver=0.2.0
pkgrel=1
pkgdesc="Linux control utility and native Qt6 GUI for Sennheiser BTD 600/700 and HDB 630 ANC"
arch=('x86_64' 'aarch64')
url="https://github.com/th4656/btd700"
license=('MIT')
depends=('qt6-base' 'bluez' 'glibc' 'gcc-libs')
makedepends=('cargo' 'rust' 'qt6-base' 'pkgconf' 'gcc' 'git')
optdepends=('bluez-utils: automatic headphone scanning via bluetoothctl')
source=("$pkgname::git+$url.git#branch=main")
sha256sums=('SKIP')

prepare() {
    cd "$srcdir/$pkgname"
    export RUSTUP_TOOLCHAIN=stable
    cargo fetch --locked --target "$(rustc -vV | sed -n 's/host: //p')"
}

build() {
    cd "$srcdir/$pkgname"
    export RUSTUP_TOOLCHAIN=stable
    export CARGO_TARGET_DIR=target
    cargo build --frozen --release --all-features
}

check() {
    cd "$srcdir/$pkgname"
    export RUSTUP_TOOLCHAIN=stable
    cargo test --frozen --all-features
}

package() {
    cd "$srcdir/$pkgname"

    # Install binary
    install -Dm755 "target/release/btd700" "$pkgdir/usr/bin/btd700"

    # Install udev rules
    install -Dm644 "99-sennheiser-btd.rules" "$pkgdir/usr/lib/udev/rules.d/70-btd700.rules"

    # Install desktop launcher and icon
    install -Dm644 "sennheiser-dongle-control.desktop" "$pkgdir/usr/share/applications/btd700.desktop"
    install -Dm644 "assets/app_icon.png" "$pkgdir/usr/share/icons/hicolor/256x256/apps/sennheiser-btd.png"

    # Install manual page
    install -Dm644 "docs/btd700.1" "$pkgdir/usr/share/man/man1/btd700.1"

    # Install documentation
    install -Dm644 "README.md" "$pkgdir/usr/share/doc/$pkgname/README.md"
    install -Dm644 "docs/ARCHITECTURE.md" "$pkgdir/usr/share/doc/$pkgname/ARCHITECTURE.md"
    install -Dm644 "docs/PROTOCOL.md" "$pkgdir/usr/share/doc/$pkgname/PROTOCOL.md"

    # Install license
    install -Dm644 "LICENSE" "$pkgdir/usr/share/licenses/$pkgname/LICENSE"
}
