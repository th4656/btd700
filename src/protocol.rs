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
