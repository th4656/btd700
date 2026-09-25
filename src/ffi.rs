//! C ABI interface for the Qt6 GUI.
#![allow(clippy::not_unsafe_ptr_arg_deref)]

use std::ffi::{CStr, CString};

use std::os::raw::c_char;
use std::sync::Mutex;
use std::time::{Duration, Instant};

use crate::constants::{AudioMode, BroadcastQuality};
use crate::device::{get_first_dongle, DongleStatus};
use crate::headset::{find_paired_headsets, get_default_mac, load_config, save_config, HeadsetState, SennheiserHeadset};

struct GlobalHeadsetState {
    headset: Option<SennheiserHeadset>,
    last_attempt: Option<Instant>,
    last_state: HeadsetState,
}

static HEADSET_MGR: Mutex<GlobalHeadsetState> = Mutex::new(GlobalHeadsetState {
    headset: None,
    last_attempt: None,
    last_state: HeadsetState {
        mac: String::new(),
        connected: false,
        anc_enabled: false,
        anc_strength: 0,
        transparency: 0,
        adaptive_anc: false,
        anti_wind: String::new(),
        bass_boost: false,
        channel: None,
    },
});

static DONGLE_MUTEX: Mutex<()> = Mutex::new(());

#[no_mangle]
pub extern "C" fn btd_free_string(s: *mut c_char) {
    if !s.is_null() {
        unsafe { drop(CString::from_raw(s)) };
    }
}

fn to_c_string(s: String) -> *mut c_char {
    CString::new(s).unwrap_or_default().into_raw()
}

#[no_mangle]
pub extern "C" fn btd_get_dongle_status_json() -> *mut c_char {
    let _guard = DONGLE_MUTEX.lock();
    let mut dev = match get_first_dongle() {
        Some(d) => d,
        None => {
            let st = DongleStatus {
                connected: false,
                ..Default::default()
            };
            return to_c_string(serde_json::to_string(&st).unwrap_or_default());
        }
    };

    if dev.open().is_err() {
        let st = DongleStatus {
            connected: false,
            ..Default::default()
        };
        return to_c_string(serde_json::to_string(&st).unwrap_or_default());
    }

    let status = dev.get_status();
    dev.close();
    to_c_string(serde_json::to_string(&status).unwrap_or_default())
}

#[no_mangle]
pub extern "C" fn btd_set_mode(mode_str: *const c_char) -> bool {
    if mode_str.is_null() {
        return false;
    }
    let s = unsafe { CStr::from_ptr(mode_str) }.to_string_lossy().to_lowercase();
    let clean = s.replace(['-', '_', ' '], "");
    let mode = match clean.as_str() {
        "gaming" | "1" => AudioMode::Gaming,
        "broadcast" | "auracast" | "bcast" | "2" => AudioMode::Broadcast,
        _ => AudioMode::HighQuality,
    };

    let _guard = DONGLE_MUTEX.lock();
    let mut dev = match get_first_dongle() {
        Some(d) => d,
        None => return false,
    };
    if dev.open().is_err() {
        return false;
    }
    let res = dev.set_audio_mode(mode).is_ok();
    dev.close();
    res
}

#[no_mangle]
pub extern "C" fn btd_set_codec(bit: u8) -> bool {
    let _guard = DONGLE_MUTEX.lock();
    let mut dev = match get_first_dongle() {
        Some(d) => d,
        None => return false,
    };
    if dev.open().is_err() {
        return false;
    }
    let res = dev.set_codec(bit);
    dev.close();
    res
}

#[no_mangle]
pub extern "C" fn btd_set_broadcast(
    enable: i32,
    name: *const c_char,
    quality: i32,
    key: *const c_char,
) -> bool {
    let _guard = DONGLE_MUTEX.lock();
    let mut dev = match get_first_dongle() {
        Some(d) => d,
        None => return false,
    };
    if dev.open().is_err() {
        return false;
    }

    let name_str = if !name.is_null() {
        Some(unsafe { CStr::from_ptr(name) }.to_string_lossy().to_string())
    } else {
        None
    };

    let key_str = if !key.is_null() {
        Some(unsafe { CStr::from_ptr(key) }.to_string_lossy().to_string())
    } else {
        None
    };

    let qual = if quality >= 0 {
        Some(BroadcastQuality::from_u8(quality as u8))
    } else {
        None
    };

    let en = if enable >= 0 {
        Some(enable != 0)
    } else {
        None
    };

    let res = dev.set_broadcast_config(en, name_str.as_deref(), qual, key_str.as_deref());
    dev.close();
    res
}

#[no_mangle]
pub extern "C" fn btd_pair() -> bool {
    let _guard = DONGLE_MUTEX.lock();
    let mut dev = match get_first_dongle() {
        Some(d) => d,
        None => return false,
    };
    if dev.open().is_err() {
        return false;
    }
    let res = dev.trigger_pairing();
    dev.close();
    res
}

#[no_mangle]
pub extern "C" fn btd_reset() -> bool {
    let _guard = DONGLE_MUTEX.lock();
    let mut dev = match get_first_dongle() {
        Some(d) => d,
        None => return false,
    };
    if dev.open().is_err() {
        return false;
    }
    let res = dev.factory_reset();
    dev.close();
    res
}

#[no_mangle]
pub extern "C" fn btd_get_headset_devices_json() -> *mut c_char {
    let devs = find_paired_headsets();
    to_c_string(serde_json::to_string(&devs).unwrap_or_default())
}

#[no_mangle]
pub extern "C" fn btd_select_headset(mac: *const c_char) -> bool {
    if mac.is_null() {
        return false;
    }
    let m = unsafe { CStr::from_ptr(mac) }.to_string_lossy().to_uppercase();
    let mut cfg = load_config();
    cfg.headset_mac = Some(m.clone());
    save_config(&cfg);

    if let Ok(mut mgr) = HEADSET_MGR.lock() {
        if let Some(mut hs) = mgr.headset.take() {
            hs.close();
        }
        mgr.headset = Some(SennheiserHeadset::new(Some(m)));
        mgr.last_attempt = None;
    }
    true
}

#[no_mangle]
pub extern "C" fn btd_get_headset_status_json() -> *mut c_char {
    let mut mgr = match HEADSET_MGR.lock() {
        Ok(m) => m,
        Err(_) => return to_c_string("{}".to_string()),
    };

    let mac = get_default_mac().unwrap_or_default();
    if mac.is_empty() {
        mgr.last_state.connected = false;
        mgr.last_state.mac = String::new();
        return to_c_string(serde_json::to_string(&mgr.last_state).unwrap_or_default());
    }

    if mgr.headset.is_none() || mgr.headset.as_ref().map(|h| &h.mac) != Some(&mac) {
        if let Some(mut old) = mgr.headset.take() {
            old.close();
        }
        mgr.headset = Some(SennheiserHeadset::new(Some(mac.clone())));
    }

    let is_connected = mgr.headset.as_ref().map(|h| h.is_connected()).unwrap_or(false);
    if !is_connected {
        let now = Instant::now();
        if let Some(last) = mgr.last_attempt {
            if now.duration_since(last) < Duration::from_secs(15) {
                return to_c_string(serde_json::to_string(&mgr.last_state).unwrap_or_default());
            }
        }
        mgr.last_attempt = Some(now);
        if let Some(hs) = mgr.headset.as_mut() {
            if hs.connect(Duration::from_millis(2000)).is_err() {
                mgr.last_state.connected = false;
                mgr.last_state.mac = mac;
                return to_c_string(serde_json::to_string(&mgr.last_state).unwrap_or_default());
            }
        }
    }

    if let Some(hs) = mgr.headset.as_mut() {
        let state = hs.get_state();
        mgr.last_state = state.clone();
        to_c_string(serde_json::to_string(&state).unwrap_or_default())
    } else {
        to_c_string(serde_json::to_string(&mgr.last_state).unwrap_or_default())
    }
}

#[no_mangle]
pub extern "C" fn btd_toggle_anc() -> bool {
    if let Ok(mut mgr) = HEADSET_MGR.lock() {
        if let Some(hs) = mgr.headset.as_mut() {
            if !hs.is_connected() {
                let _ = hs.connect(Duration::from_millis(2500));
            }
            if hs.is_connected() {
                let new_st = hs.toggle_anc();
                mgr.last_state.anc_enabled = new_st;
                return true;
            }
        }
    }
    false
}

#[no_mangle]
pub extern "C" fn btd_set_anc(enabled: bool) -> bool {
    if let Ok(mut mgr) = HEADSET_MGR.lock() {
        if let Some(hs) = mgr.headset.as_mut() {
            if !hs.is_connected() {
                let _ = hs.connect(Duration::from_millis(2500));
            }
            if hs.is_connected() {
                let ok = hs.set_anc_status(enabled);
                mgr.last_state.anc_enabled = enabled;
                return ok;
            }
        }
    }
    false
}

#[no_mangle]
pub extern "C" fn btd_set_anc_strength(strength: i32) -> bool {
    if let Ok(mut mgr) = HEADSET_MGR.lock() {
        if let Some(hs) = mgr.headset.as_mut() {
            if !hs.is_connected() {
                let _ = hs.connect(Duration::from_millis(2500));
            }
            if hs.is_connected() {
                let ok = hs.set_anc_strength(strength);
                mgr.last_state.anc_strength = strength;
                mgr.last_state.transparency = 100 - strength;
                mgr.last_state.anc_enabled = true;
                return ok;
            }
        }
    }
    false
}

#[no_mangle]
pub extern "C" fn btd_set_transparency(level: i32) -> bool {
    if let Ok(mut mgr) = HEADSET_MGR.lock() {
        if let Some(hs) = mgr.headset.as_mut() {
            if !hs.is_connected() {
                let _ = hs.connect(Duration::from_millis(2500));
            }
            if hs.is_connected() {
                let ok = hs.set_transparency(level);
                mgr.last_state.transparency = level;
                mgr.last_state.anc_strength = 100 - level;
                return ok;
            }
        }
    }
    false
}

#[no_mangle]
pub extern "C" fn btd_set_adaptive_anc(enabled: bool) -> bool {
    if let Ok(mut mgr) = HEADSET_MGR.lock() {
        if let Some(hs) = mgr.headset.as_mut() {
            if !hs.is_connected() {
                let _ = hs.connect(Duration::from_millis(2500));
            }
            if hs.is_connected() {
                let ok = hs.set_adaptive_anc(enabled);
                mgr.last_state.adaptive_anc = enabled;
                return ok;
            }
        }
    }
    false
}

#[no_mangle]
pub extern "C" fn btd_set_anti_wind(mode: *const c_char) -> bool {
    if mode.is_null() {
        return false;
    }
    let m = unsafe { CStr::from_ptr(mode) }.to_string_lossy().to_string();
    if let Ok(mut mgr) = HEADSET_MGR.lock() {
        if let Some(hs) = mgr.headset.as_mut() {
            if !hs.is_connected() {
                let _ = hs.connect(Duration::from_millis(2500));
            }
            if hs.is_connected() {
                let ok = hs.set_anti_wind(&m);
                mgr.last_state.anti_wind = m;
                return ok;
            }
        }
    }
    false
}

#[no_mangle]
pub extern "C" fn btd_set_bass_boost(enabled: bool) -> bool {
    if let Ok(mut mgr) = HEADSET_MGR.lock() {
        if let Some(hs) = mgr.headset.as_mut() {
            if !hs.is_connected() {
                let _ = hs.connect(Duration::from_millis(2500));
            }
            if hs.is_connected() {
                let ok = hs.set_bass_boost(enabled);
                mgr.last_state.bass_boost = enabled;
                return ok;
            }
        }
    }
    false
}
