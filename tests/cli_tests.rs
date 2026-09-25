use btd700::cli::{Cli, Commands};
use clap::Parser;

#[test]
fn test_cli_parse_empty() {
    let cli = Cli::try_parse_from(["btd700"]);
    assert!(cli.is_ok());
    assert!(cli.unwrap().command.is_none());
}

#[test]
fn test_cli_parse_list() {
    let cli = Cli::try_parse_from(["btd700", "list"]).unwrap();
    match cli.command {
        Some(Commands::List) => (),
        other => panic!("Expected List command, got {:?}", other),
    }
}

#[test]
fn test_cli_parse_status() {
    let cli = Cli::try_parse_from(["btd700", "status"]).unwrap();
    match cli.command {
        Some(Commands::Status) => (),
        other => panic!("Expected Status command, got {:?}", other),
    }
}

#[test]
fn test_cli_parse_mode() {
    let cli = Cli::try_parse_from(["btd700", "mode", "gaming"]).unwrap();
    match cli.command {
        Some(Commands::Mode { mode }) => assert_eq!(mode, "gaming"),
        other => panic!("Expected Mode command, got {:?}", other),
    }
}

#[test]
fn test_cli_parse_codec() {
    let cli = Cli::try_parse_from(["btd700", "codec", "aptx-adaptive"]).unwrap();
    match cli.command {
        Some(Commands::Codec { codec }) => assert_eq!(codec, Some("aptx-adaptive".to_string())),
        other => panic!("Expected Codec command, got {:?}", other),
    }
}

#[test]
fn test_cli_parse_anc() {
    let cli = Cli::try_parse_from(["btd700", "anc", "toggle"]).unwrap();
    match cli.command {
        Some(Commands::Anc { state, mac }) => {
            assert_eq!(state, Some("toggle".to_string()));
            assert_eq!(mac, None);
        }
        other => panic!("Expected Anc command, got {:?}", other),
    }
}

#[test]
fn test_cli_parse_anc_strength() {
    let cli = Cli::try_parse_from(["btd700", "anc-strength", "85", "--mac", "AA:BB:CC:DD:EE:FF"]).unwrap();
    match cli.command {
        Some(Commands::AncStrength { strength, mac }) => {
            assert_eq!(strength, Some(85));
            assert_eq!(mac, Some("AA:BB:CC:DD:EE:FF".to_string()));
        }
        other => panic!("Expected AncStrength command, got {:?}", other),
    }
}

#[test]
fn test_cli_parse_broadcast() {
    let cli = Cli::try_parse_from([
        "btd700",
        "broadcast",
        "--enable",
        "--name",
        "Test Stream",
        "--quality",
        "hq",
    ])
    .unwrap();
    match cli.command {
        Some(Commands::Broadcast { enable, name, quality, .. }) => {
            assert!(enable);
            assert_eq!(name, Some("Test Stream".to_string()));
            assert_eq!(quality, Some("hq".to_string()));
        }
        other => panic!("Expected Broadcast command, got {:?}", other),
    }
}

#[test]
fn test_cli_parse_gui() {
    let cli = Cli::try_parse_from(["btd700", "gui"]).unwrap();
    match cli.command {
        Some(Commands::Gui { tray }) => assert!(!tray),
        other => panic!("Expected Gui command, got {:?}", other),
    }

    let cli_tray = Cli::try_parse_from(["btd700", "gui", "--tray"]).unwrap();
    match cli_tray.command {
        Some(Commands::Gui { tray }) => assert!(tray),
        other => panic!("Expected Gui command with tray, got {:?}", other),
    }
}

