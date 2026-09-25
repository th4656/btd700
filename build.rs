use std::env;
use std::path::{Path, PathBuf};
use std::process::Command;

fn main() {
    println!("cargo:rerun-if-changed=gui/");
    println!("cargo:rerun-if-changed=build.rs");

    let qt = pkg_config::probe_library("Qt6Widgets").expect("Qt6Widgets not found via pkg-config");
    let _qt_core = pkg_config::probe_library("Qt6Core").expect("Qt6Core not found via pkg-config");
    let _qt_gui = pkg_config::probe_library("Qt6Gui").expect("Qt6Gui not found via pkg-config");

    let out_dir = PathBuf::from(env::var("OUT_DIR").unwrap());

    // Locate moc binary
    let moc_candidates = ["/usr/lib/qt6/moc", "/usr/bin/moc-qt6", "/usr/bin/moc", "moc"];
    let mut moc_bin = "moc";
    for cand in &moc_candidates {
        if Path::new(cand).exists() {
            moc_bin = cand;
            break;
        }
    }

    let moc_out = out_dir.join("moc_main_window.cpp");
    let _ = Command::new(moc_bin)
        .arg("gui/main_window.h")
        .arg("-o")
        .arg(&moc_out)
        .status();

    let mut builder = cc::Build::new();
    builder
        .cpp(true)
        .std("c++17")
        .file("gui/gui.cpp")
        .file("gui/main_window.cpp");

    if moc_out.exists() {
        builder.file(&moc_out);
    }

    for inc in &qt.include_paths {
        builder.include(inc);
    }
    builder.include(&out_dir);
    builder.include("gui");

    builder.compile("btd700_gui");
}
