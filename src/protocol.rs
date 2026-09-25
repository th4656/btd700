//! Protocol packet framing and parser for Sennheiser BTD 600 / BTD 700.

use crate::constants::{HostCmd, BTD700_REPORT_ID};

pub struct Protocol;

impl Protocol {
    /// Builds a host command packet for BTD Report ID 52.
    /// Format: `[52, 0xFE, cmd_id, args_len, ...args]`
    pub fn build_host_cmd(cmd: HostCmd, args: &[u8]) -> Vec<u8> {
        let mut packet = Vec::with_capacity(4 + args.len());
        packet.push(BTD700_REPORT_ID);
        packet.push(0xFE);
        packet.push(cmd as u8);
        packet.push(args.len() as u8);
        packet.extend_from_slice(args);
        packet
    }

    /// Parses a dongle response packet.
    /// Expected format: `[52, 0xFD, cmd_id, payload_len, ...payload]`
    pub fn parse_response(data: &[u8]) -> Option<(u8, Vec<u8>)> {
        if data.len() < 4 {
            return None;
        }

        let mut offset = 0;
        if data[0] == BTD700_REPORT_ID {
            offset = 1;
        }

        if data.len() < offset + 3 {
            return None;
        }

        let magic = data[offset];
        if magic != 0xFD {
            return None;
        }

        let cmd_id = data[offset + 1];
        let len = data[offset + 2] as usize;
        let payload_start = offset + 3;

        if data.len() < payload_start + len {
            // Take what is available
            let payload = data[payload_start..].to_vec();
            return Some((cmd_id, payload));
        }

        let payload = data[payload_start..payload_start + len].to_vec();
        Some((cmd_id, payload))
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_build_host_cmd_no_args() {
        let pkt = Protocol::build_host_cmd(HostCmd::GetDongleState, &[]);
        assert_eq!(pkt, vec![52, 0xFE, HostCmd::GetDongleState as u8, 0]);
    }

    #[test]
    fn test_build_host_cmd_with_args() {
        let pkt = Protocol::build_host_cmd(HostCmd::SetAudioModeAndTransport, &[1, 2]);
        assert_eq!(pkt, vec![52, 0xFE, 2, 2, 1, 2]);
    }

    #[test]
    fn test_parse_response_with_report_id() {
        // [52, 0xFD, cmd_id=6, len=2, 0xAA, 0xBB]
        let data = [52, 0xFD, 6, 2, 0xAA, 0xBB, 0x00, 0x00];
        let res = Protocol::parse_response(&data);
        assert!(res.is_some());
        let (cmd, payload) = res.unwrap();
        assert_eq!(cmd, 6);
        assert_eq!(payload, vec![0xAA, 0xBB]);
    }

    #[test]
    fn test_parse_response_without_report_id() {
        // [0xFD, cmd_id=1, len=1, 0x01]
        let data = [0xFD, 1, 1, 0x01];
        let res = Protocol::parse_response(&data);
        assert!(res.is_some());
        let (cmd, payload) = res.unwrap();
        assert_eq!(cmd, 1);
        assert_eq!(payload, vec![0x01]);
    }

    #[test]
    fn test_parse_response_invalid_magic() {
        let data = [52, 0xAA, 1, 1, 0x01];
        assert_eq!(Protocol::parse_response(&data), None);
    }

    #[test]
    fn test_parse_response_too_short() {
        let data = [52, 0xFD];
        assert_eq!(Protocol::parse_response(&data), None);
    }
}

