//! Command Line Interface (CLI) implementation with Clap.

use clap::{Parser, Subcommand};
use std::time::Duration;

use crate::constants::{AudioMode, BroadcastQuality, CodecBit};
use crate::device::get_first_dongle;
use crate::headset::{find_paired_headsets, get_default_mac, SennheiserHeadset};
use crate::cloud::SennheiserCloudClient;
use crate::dfu::DFUImage;

#[derive(Parser, Debug)]
#[command(
    name = "btd700",
    about = "Sennheiser Dongle Control (BTD 600 / BTD 700 & Headset ANC) in Rust with Qt6 GUI",
    version = "0.2.0"
)]
pub struct Cli {
    #[command(subcommand)]
    pub command: Option<Commands>,
}

#[derive(Subcommand, Debug)]
pub enum Commands {
    /// List detected Sennheiser BTD dongles
    List,

    /// Query comprehensive dongle status, audio mode, and active codec
    Status,

    /// Switch audio link mode (one-to-one, gaming, broadcast)
    Mode {
        /// Mode name: one-to-one (hq), gaming, broadcast (auracast)
        mode: String,
    },

    /// View or switch active audio codec
    Codec {
        /// Codec name: sbc, aptx, aptx-adaptive, aptx-lossless, aptx-lite, lc3
        codec: Option<String>,
    },

    /// Configure Auracast broadcast parameters
    Broadcast {
        #[arg(long)]
        enable: bool,

        #[arg(long)]
        disable: bool,

        #[arg(long)]
        name: Option<String>,

        #[arg(long)]
        quality: Option<String>,

        #[arg(long)]
        key: Option<String>,
    },

    /// View, toggle, or set Active Noise Cancellation (ANC) on headset
    Anc {
        /// ANC state: on, off, or toggle
        state: Option<String>,

        #[arg(long)]
        mac: Option<String>,
    },

    /// View or set ANC strength percentage (0-100%)
    #[command(name = "anc-strength", alias = "strength")]
    AncStrength {
        /// Strength percentage (0-100)
        strength: Option<i32>,

        #[arg(long)]
        mac: Option<String>,
    },

    /// View or set Adaptive ANC mode (automatic environmental adaptation)
    #[command(name = "adaptive-anc", alias = "adaptive")]
    AdaptiveAnc {
        /// State: on (auto) or off (manual)
        state: Option<String>,

        #[arg(long)]
        mac: Option<String>,
    },

    /// View or set Anti-Wind reduction mode
    #[command(name = "anti-wind", alias = "wind")]
    AntiWind {
        /// Mode: off, auto, max
        mode: Option<String>,

        #[arg(long)]
        mac: Option<String>,
    },

    /// View or set transparency mode level (0-100%)
    Transparency {
        /// Transparency level percentage (0-100)
        level: Option<i32>,

        #[arg(long)]
        mac: Option<String>,
    },

    /// View or set Bass Boost hardware EQ on headset
    #[command(name = "bass-boost")]
    BassBoost {
        /// State: on or off
        state: Option<String>,

        #[arg(long)]
        mac: Option<String>,
    },

    /// Show connected Sennheiser headset status summary
    Headset {
        #[arg(long)]
        mac: Option<String>,
    },

    /// List paired Bluetooth headsets
    #[command(name = "headsets", alias = "scan")]
    Headsets,

    /// Trigger Bluetooth connect / pairing mode on dongle
    Pair,

    /// Factory reset dongle
    Reset,

    /// Firmware update management (DFU)
    Dfu {
        /// Action: check or update
        action: String,

        #[arg(long)]
        file: Option<String>,
    },

    /// Launch the native Qt6 GUI Control Panel
    #[command(name = "gui")]
    Gui,
}

pub fn run_cli(cli: Cli) -> Result<(), Box<dyn std::error::Error>> {
    let cmd = match cli.command {
        Some(c) => c,
        None => {
            // Default to running GUI if no arguments passed
            return run_gui();
        }
    };

    match cmd {
        Commands::List => {
            let devs = crate::hid::find_sennheiser_hid_devices();
            println!("\x1b[1;36m━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\x1b[0m");
            println!("  \x1b[1mSennheiser Dongles Detected\x1b[0m");
            println!("\x1b[1;36m━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\x1b[0m");
            if devs.is_empty() {
                println!("No Sennheiser BTD dongles detected.");
                println!("Make sure the dongle is plugged in and udev rules are installed.");
                return Ok(());
            }
            for (idx, d) in devs.iter().enumerate() {
                println!("[{idx}] {} ({})", d.model_name, d.product_name);
                println!("    Path:         {}", d.dev_path);
                println!("    VID/PID:      0x{:04X}:0x{:04X}", d.vid, d.pid);
                if !d.serial.is_empty() {
                    println!("    Serial:       {}", d.serial);
                }
                println!("    Report IDs:   {:?}", d.report_ids);
            }
        }

        Commands::Status => {
            let mut dev = match get_first_dongle() {
                Some(d) => d,
                None => {
                    eprintln!("Error: No Sennheiser BTD dongle found.");
                    return Ok(());
                }
            };
            dev.open()?;
            let status = dev.get_status();
            dev.close();

            println!("\x1b[1;36m━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\x1b[0m");
            println!("  \x1b[1m{} Status\x1b[0m", status.model);
            println!("\x1b[1;36m━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\x1b[0m");
            println!("  Connection:       \x1b[1;32mConnected\x1b[0m");
            if !status.serial.is_empty() {
                println!("  Serial:           {}", status.serial);
            }
            println!("  Hardware ID:      {}:{}", status.vid, status.pid);
            if !status.dongle_state.is_empty() {
                println!("  Dongle State:     \x1b[1;32m{}\x1b[0m", status.dongle_state);
            }
            if !status.audio_mode.is_empty() {
                println!("  Audio Mode:       \x1b[1;35m{}\x1b[0m", status.audio_mode);
            }
            if !status.transport_mode.is_empty() {
                println!("  Transport:        {}", status.transport_mode);
            }
            if !status.codec_in_use.is_empty() {
                println!("  Active Codec:     \x1b[1;33m{}\x1b[0m", status.codec_in_use);
            }
            if !status.frequency.is_empty() && !status.resolution.is_empty() {
                println!("  Audio Quality:    {} / {}", status.frequency, status.resolution);
            }
            if !status.supported_codecs.is_empty() {
                let names: Vec<String> = status.supported_codecs.iter().map(|c| c.name.clone()).collect();
                println!("  Supported Codecs: {}", names.join(", "));
            }
            if status.broadcast_enabled || !status.broadcast_name.is_empty() {
                let b_st = if status.broadcast_enabled { "\x1b[1;32mON (Public)\x1b[0m" } else { "OFF (Private)" };
                println!("\n  [Auracast Broadcast]");
                println!("    State:          {b_st}");
                println!("    Name:           {}", status.broadcast_name);
                println!("    Quality:        {}", status.broadcast_quality);
                println!("    Encryption:     {}", if status.broadcast_encrypted { "Encrypted" } else { "Open" });
            }
        }

        Commands::Mode { mode } => {
            let mut dev = match get_first_dongle() {
                Some(d) => d,
                None => {
                    eprintln!("Error: No Sennheiser BTD dongle found.");
                    return Ok(());
                }
            };
            let clean = mode.to_lowercase().replace(['-', '_', ' '], "");
            let m = match clean.as_str() {
                "gaming" | "1" => AudioMode::Gaming,
                "broadcast" | "auracast" | "bcast" | "2" => AudioMode::Broadcast,
                "onetoone" | "hq" | "highquality" | "0" => AudioMode::HighQuality,
                other => {
                    eprintln!("Unknown mode '{other}'. Choose: one-to-one (hq), gaming, broadcast (auracast)");
                    dev.close();
                    return Ok(());
                }
            };
            if dev.set_audio_mode(m) {
                println!("Audio mode switched to: \x1b[1;32m{}\x1b[0m", m.as_str());
            } else {
                eprintln!("Failed to set audio mode.");
            }
            dev.close();
        }

        Commands::Codec { codec } => {
            let mut dev = match get_first_dongle() {
                Some(d) => d,
                None => {
                    eprintln!("Error: No Sennheiser BTD dongle found.");
                    return Ok(());
                }
            };
            dev.open()?;
            if let Some(c_name) = codec {
                if let Some(cb) = CodecBit::from_name(&c_name) {
                    if dev.set_codec(cb as u8) {
                        println!("Codec switched to: \x1b[1;32m{}\x1b[0m", cb.name());
                    } else {
                        eprintln!("Failed to switch codec.");
                    }
                } else {
                    eprintln!("Unknown codec '{c_name}'. Choose: sbc, aptx, aptx-adaptive, aptx-lossless, aptx-lite, lc3");
                }
            } else {
                let st = dev.get_status();
                println!("Active Codec: \x1b[1;33m{}\x1b[0m", st.codec_in_use);
            }
            dev.close();
        }

        Commands::Broadcast { enable, disable, name, quality, key } => {
            let mut dev = match get_first_dongle() {
                Some(d) => d,
                None => {
                    eprintln!("Error: No Sennheiser BTD dongle found.");
                    return Ok(());
                }
            };
            dev.open()?;
            let en = if enable {
                Some(true)
            } else if disable {
                Some(false)
            } else {
                None
            };
            let qual = quality.as_deref().and_then(|q| match q.to_lowercase().as_str() {
                "sq16" => Some(BroadcastQuality::Sq16),
                "sq24" => Some(BroadcastQuality::Sq24),
                "hq" => Some(BroadcastQuality::Hq),
                _ => None,
            });

            if dev.set_broadcast_config(en, name.as_deref(), qual, key.as_deref()) {
                println!("Auracast broadcast settings updated successfully.");
            } else {
                eprintln!("Failed to update broadcast settings.");
            }
            dev.close();
        }

        Commands::Anc { state, mac } => {
            let mut hs = SennheiserHeadset::new(mac);
            hs.connect(Duration::from_millis(2500))?;
            if let Some(st) = state {
                match st.to_lowercase().as_str() {
                    "on" => {
                        hs.set_anc_status(true);
                        println!("Active Noise Cancellation (ANC): \x1b[1;32mON\x1b[0m");
                    }
                    "off" => {
                        hs.set_anc_status(false);
                        println!("Active Noise Cancellation (ANC): \x1b[1;31mOFF\x1b[0m");
                    }
                    "toggle" => {
                        let new_st = hs.toggle_anc();
                        let color = if new_st { "\x1b[1;32mON\x1b[0m" } else { "\x1b[1;31mOFF\x1b[0m" };
                        println!("Active Noise Cancellation (ANC) toggled to: {color}");
                    }
                    _ => eprintln!("Invalid ANC state. Choose: on, off, toggle"),
                }
            } else {
                let is_on = hs.get_anc_status();
                let color = if is_on { "\x1b[1;32mON\x1b[0m" } else { "\x1b[1;31mOFF\x1b[0m" };
                println!("Active Noise Cancellation (ANC): {color}");
            }
            hs.close();
        }

        Commands::AncStrength { strength, mac } => {
            let mut hs = SennheiserHeadset::new(mac);
            hs.connect(Duration::from_millis(2500))?;
            if let Some(val) = strength {
                hs.set_anc_strength(val);
                println!("ANC Strength set to: \x1b[1;32m{val}%\x1b[0m (Transparency: {}%)", 100 - val);
            } else {
                let val = hs.get_anc_strength();
                println!("ANC Strength: \x1b[1;32m{val}%\x1b[0m");
            }
            hs.close();
        }

        Commands::AdaptiveAnc { state, mac } => {
            let mut hs = SennheiserHeadset::new(mac);
            hs.connect(Duration::from_millis(2500))?;
            if let Some(st) = state {
                let en = st.to_lowercase() == "on";
                hs.set_adaptive_anc(en);
                let label = if en { "\x1b[1;32mON (Auto)\x1b[0m" } else { "\x1b[1;33mOFF (Manual)\x1b[0m" };
                println!("Adaptive ANC set to: {label}");
            } else {
                let adapt = hs.get_adaptive_anc();
                let label = if adapt { "\x1b[1;32mON (Auto)\x1b[0m" } else { "\x1b[1;33mOFF (Manual)\x1b[0m" };
                println!("Adaptive ANC: {label}");
            }
            hs.close();
        }

        Commands::AntiWind { mode, mac } => {
            let mut hs = SennheiserHeadset::new(mac);
            hs.connect(Duration::from_millis(2500))?;
            if let Some(m) = mode {
                hs.set_anti_wind(&m);
                println!("Anti-Wind Reduction set to: \x1b[1;32m{}\x1b[0m", m.to_uppercase());
            } else {
                let m = hs.get_anti_wind();
                println!("Anti-Wind Reduction: \x1b[1;36m{}\x1b[0m", m.to_uppercase());
            }
            hs.close();
        }

        Commands::Transparency { level, mac } => {
            let mut hs = SennheiserHeadset::new(mac);
            hs.connect(Duration::from_millis(2500))?;
            if let Some(lvl) = level {
                hs.set_transparency(lvl);
                println!("Transparency Level set to: \x1b[1;32m{lvl}%\x1b[0m");
            } else {
                let lvl = hs.get_transparency();
                println!("Transparency Level: \x1b[1;36m{lvl}%\x1b[0m");
            }
            hs.close();
        }

        Commands::BassBoost { state, mac } => {
            let mut hs = SennheiserHeadset::new(mac);
            hs.connect(Duration::from_millis(2500))?;
            if let Some(st) = state {
                let en = st.to_lowercase() == "on";
                hs.set_bass_boost(en);
                let color = if en { "\x1b[1;32mON\x1b[0m" } else { "\x1b[1;31mOFF\x1b[0m" };
                println!("Bass Boost set to: {color}");
            } else {
                let is_on = hs.get_bass_boost();
                let color = if is_on { "\x1b[1;32mON\x1b[0m" } else { "\x1b[1;31mOFF\x1b[0m" };
                println!("Bass Boost: {color}");
            }
            hs.close();
        }

        Commands::Headset { mac } => {
            let mut hs = SennheiserHeadset::new(mac);
            hs.connect(Duration::from_millis(2500))?;
            let st = hs.get_state();
            hs.close();

            println!("\x1b[1;36m━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\x1b[0m");
            println!("  \x1b[1mSennheiser Headset Status\x1b[0m");
            println!("\x1b[1;36m━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\x1b[0m");
            println!("  MAC Address:      {}", st.mac);
            let anc_st = if st.anc_enabled { "\x1b[1;32mON\x1b[0m" } else { "\x1b[1;31mOFF\x1b[0m" };
            println!("  ANC State:        {anc_st}");
            println!("  ANC Strength:     \x1b[1;32m{}%\x1b[0m", st.anc_strength);
            println!("  Transparency:     \x1b[1;36m{}%\x1b[0m", st.transparency);
            let adapt_st = if st.adaptive_anc { "\x1b[1;32mON (Auto)\x1b[0m" } else { "\x1b[1;33mOFF (Manual)\x1b[0m" };
            println!("  Adaptive ANC:     {adapt_st}");
            println!("  Anti-Wind Mode:   \x1b[1;36m{}\x1b[0m", st.anti_wind.to_uppercase());
            let bb_st = if st.bass_boost { "\x1b[1;32mON\x1b[0m" } else { "\x1b[1;31mOFF\x1b[0m" };
            println!("  Bass Boost:       {bb_st}");
        }

        Commands::Headsets => {
            let devs = find_paired_headsets();
            println!("\x1b[1;36m━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\x1b[0m");
            println!("  \x1b[1mPaired Bluetooth Devices\x1b[0m");
            println!("\x1b[1;36m━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\x1b[0m");
            if devs.is_empty() {
                println!("No paired Bluetooth devices found.");
                return Ok(());
            }
            let def_mac = get_default_mac().unwrap_or_default();
            for d in devs {
                let rec = if d.recommended { " \x1b[1;32m[Recommended / Sennheiser]\x1b[0m" } else { "" };
                let active = if d.mac == def_mac { " \x1b[1;36m(Current Default)\x1b[0m" } else { "" };
                println!("  {} - {}{rec}{active}", d.mac, d.name);
            }
        }

        Commands::Pair => {
            let mut dev = match get_first_dongle() {
                Some(d) => d,
                None => {
                    eprintln!("Error: No Sennheiser BTD dongle found.");
                    return Ok(());
                }
            };
            dev.open()?;
            if dev.trigger_pairing() {
                println!("Triggered Bluetooth pairing mode on dongle.");
            } else {
                eprintln!("Failed to trigger pairing mode.");
            }
            dev.close();
        }

        Commands::Reset => {
            let mut dev = match get_first_dongle() {
                Some(d) => d,
                None => {
                    eprintln!("Error: No Sennheiser BTD dongle found.");
                    return Ok(());
                }
            };
            dev.open()?;
            if dev.factory_reset() {
                println!("Dongle restored to factory settings and restarting.");
            } else {
                eprintln!("Failed to factory reset dongle.");
            }
            dev.close();
        }

        Commands::Dfu { action, file } => {
            let client = SennheiserCloudClient::new();
            let sku = "700434"; // BTD 700 SKU

            if action == "check" {
                println!("Checking Sennheiser cloud for firmware updates...");
                let versions = client.get_available_versions(sku)?;
                if let Some(latest) = versions.first() {
                    println!("Latest firmware version: \x1b[1;32m{latest}\x1b[0m");
                    if let Ok(notes) = client.get_release_notes(sku, latest) {
                        println!("\nRelease Notes:\n{notes}");
                    }
                } else {
                    println!("No firmware versions found for SKU {sku}.");
                }
            } else if action == "update" {
                if let Some(fpath) = file {
                    let img = DFUImage::load(fpath)?;
                    println!("Loaded DFU image: {} ({})", img.chip_model, img.firmware_version);
                } else {
                    println!("Fetching latest firmware package from Sennheiser cloud...");
                    let versions = client.get_available_versions(sku)?;
                    let latest = versions.first().ok_or("No firmware releases available")?;
                    let url = client.get_dfu_download_url(sku, latest)?;
                    let dest = format!("/tmp/btd_dfu_{latest}.bin");
                    client.download_firmware(&url, std::path::Path::new(&dest))?;
                    println!("Downloaded firmware {latest} to {dest}");
                }
            }
        }

        Commands::Gui => {
            return run_gui();
        }
    }

    Ok(())
}

pub fn run_gui() -> Result<(), Box<dyn std::error::Error>> {
    unsafe extern "C" {
        fn run_qt_gui_app(argc: libc::c_int, argv: *const *const libc::c_char) -> libc::c_int;
    }

    let arg0 = std::ffi::CString::new("btd700").unwrap();
    let argv = [arg0.as_ptr(), std::ptr::null()];
    unsafe {
        run_qt_gui_app(1, argv.as_ptr());
    }
    Ok(())
}
