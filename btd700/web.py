"""
Lightweight Web GUI Control Panel for Sennheiser BTD 600 / BTD 700 on Linux.
Faithfully recreates the Sennheiser Dongle Control interface with dark mode,
modern responsive design, real-time controls, and REST API.
"""

import os
import json
import webbrowser
import threading
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse, parse_qs
from typing import Optional, Dict, Any

from .constants import (
    AudioMode,
    TransportMode,
    CodecBit,
    CODEC_NAMES,
    BroadcastQuality,
    DeviceType,
)
from .device import BTDDevice, get_first_dongle
from .hid import find_sennheiser_hid_devices
from .cloud import SennheiserCloudClient


HTML_TEMPLATE = """<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Sennheiser Dongle Control</title>
  <link rel="icon" type="image/png" href="/assets/app_icon.png">
  <style>
    :root {
      --bg-primary: #121316;
      --bg-secondary: #1a1c23;
      --bg-card: #22252e;
      --bg-card-hover: #292d38;
      --accent: #009fe3;
      --accent-gradient: linear-gradient(135deg, #009fe3 0%, #0077b6 100%);
      --accent-glow: rgba(0, 159, 227, 0.35);
      --text-main: #f0f2f5;
      --text-muted: #8e95a5;
      --border-color: #2e3340;
      --success: #10b981;
      --warning: #f59e0b;
      --danger: #ef4444;
      --radius: 12px;
    }

    * { box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif; }
    body { background-color: var(--bg-primary); color: var(--text-main); min-height: 100vh; display: flex; flex-direction: column; }

    header {
      background: var(--bg-secondary);
      border-bottom: 1px solid var(--border-color);
      padding: 16px 32px;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .brand {
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .brand img { width: 32px; height: 32px; object-fit: contain; }
    .brand h1 { font-size: 1.15rem; font-weight: 600; letter-spacing: 0.5px; }

    .status-badge {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      background: rgba(16, 185, 129, 0.15);
      color: var(--success);
      padding: 6px 14px;
      border-radius: 20px;
      font-size: 0.85rem;
      font-weight: 500;
    }
    .status-badge.disconnected {
      background: rgba(239, 68, 68, 0.15);
      color: var(--danger);
    }
    .status-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: currentColor;
    }

    main {
      flex: 1;
      max-width: 960px;
      width: 100%;
      margin: 0 auto;
      padding: 32px 24px;
      display: flex;
      flex-direction: column;
      gap: 28px;
    }

    /* Device Hero Section */
    .hero-card {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: var(--radius);
      padding: 24px 32px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 24px;
    }
    .hero-info h2 { font-size: 1.6rem; font-weight: 600; margin-bottom: 6px; }
    .hero-info p { color: var(--text-muted); font-size: 0.9rem; }
    .hero-img { height: 90px; object-fit: contain; filter: drop-shadow(0 6px 16px rgba(0,0,0,0.5)); }

    /* Section Headers */
    .section-title {
      font-size: 1rem;
      font-weight: 600;
      color: var(--text-muted);
      text-transform: uppercase;
      letter-spacing: 0.8px;
      margin-bottom: 12px;
    }

    /* Audio Mode Grid */
    .modes-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 16px;
    }
    .mode-card {
      background: var(--bg-card);
      border: 2px solid transparent;
      border-radius: var(--radius);
      padding: 20px;
      cursor: pointer;
      transition: all 0.2s ease;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .mode-card:hover {
      background: var(--bg-card-hover);
      transform: translateY(-2px);
    }
    .mode-card.active {
      border-color: var(--accent);
      background: rgba(0, 159, 227, 0.08);
      box-shadow: 0 0 20px var(--accent-glow);
    }
    .mode-card h3 { font-size: 1.1rem; font-weight: 600; }
    .mode-card p { font-size: 0.85rem; color: var(--text-muted); line-height: 1.4; }

    /* Details Panel */
    .details-grid {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 16px;
    }
    .panel-card {
      background: var(--bg-card);
      border: 1px solid var(--border-color);
      border-radius: var(--radius);
      padding: 20px;
    }

    /* Codecs */
    .codecs-list {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-top: 10px;
    }
    .codec-badge {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      padding: 8px 14px;
      border-radius: 8px;
      font-size: 0.85rem;
      cursor: pointer;
      transition: all 0.15s ease;
    }
    .codec-badge:hover {
      border-color: var(--text-muted);
    }
    .codec-badge.active {
      background: var(--accent);
      color: #fff;
      border-color: var(--accent);
      font-weight: 600;
    }

    /* Stats List */
    .stat-row {
      display: flex;
      justify-content: space-between;
      padding: 8px 0;
      border-bottom: 1px solid rgba(255,255,255,0.05);
      font-size: 0.9rem;
    }
    .stat-row:last-child { border-bottom: none; }
    .stat-label { color: var(--text-muted); }
    .stat-val { font-weight: 500; }

    /* Auracast Broadcast Panel */
    .bcast-panel {
      background: var(--bg-card);
      border: 1px solid var(--border-color);
      border-radius: var(--radius);
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }
    .input-group {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .input-group label {
      font-size: 0.85rem;
      color: var(--text-muted);
    }
    .input-group input, .input-group select {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      color: var(--text-main);
      padding: 10px 14px;
      border-radius: 8px;
      font-size: 0.9rem;
      outline: none;
    }
    .input-group input:focus, .input-group select:focus {
      border-color: var(--accent);
    }

    /* Button Bar */
    .btn-row {
      display: flex;
      gap: 12px;
      margin-top: 8px;
    }
    .btn {
      padding: 10px 20px;
      border-radius: 8px;
      font-size: 0.9rem;
      font-weight: 600;
      cursor: pointer;
      border: none;
      transition: all 0.15s ease;
      display: inline-flex;
      align-items: center;
      gap: 8px;
    }
    .btn-primary {
      background: var(--accent-gradient);
      color: #fff;
    }
    .btn-primary:hover { opacity: 0.9; }
    .btn-secondary {
      background: var(--bg-card);
      border: 1px solid var(--border-color);
      color: var(--text-main);
    }
    .btn-secondary:hover { background: var(--bg-card-hover); }
    .btn-danger {
      background: rgba(239, 68, 68, 0.15);
      color: var(--danger);
      border: 1px solid rgba(239, 68, 68, 0.3);
    }
    .btn-danger:hover { background: var(--danger); color: #fff; }

    /* Modal */
    .modal-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0,0,0,0.7);
      backdrop-filter: blur(4px);
      display: none;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }
    .modal-overlay.open { display: flex; }
    .modal-box {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: var(--radius);
      width: 90%;
      max-width: 520px;
      padding: 24px;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }
    .modal-title { font-size: 1.2rem; font-weight: 600; }
    .notes-box {
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 12px;
      max-height: 200px;
      overflow-y: auto;
      font-size: 0.85rem;
      white-space: pre-wrap;
      color: var(--text-muted);
    }

    footer {
      text-align: center;
      padding: 20px;
      color: var(--text-muted);
      font-size: 0.8rem;
      border-top: 1px solid var(--border-color);
    }
  </style>
</head>
<body>
  <header>
    <div class="brand">
      <img src="/assets/app_icon.png" alt="Sennheiser Icon">
      <h1>Sennheiser Dongle Control</h1>
    </div>
    <div id="statusBadge" class="status-badge">
      <span class="status-dot"></span>
      <span id="statusText">Checking...</span>
    </div>
  </header>

  <main>
    <!-- Hero Card -->
    <div class="hero-card">
      <div class="hero-info">
        <h2 id="deviceModel">BTD 700</h2>
        <p id="deviceSub">High-Resolution Bluetooth USB Adapter</p>
        <p style="margin-top: 6px; font-size: 0.8rem; color: var(--text-muted);" id="deviceSerial">Serial: ...</p>
      </div>
      <img id="deviceImg" class="hero-img" src="/assets/btd700.png" alt="BTD Dongle">
    </div>

    <!-- Audio Modes -->
    <div>
      <div class="section-title">Audio Mode</div>
      <div class="modes-grid">
        <div class="mode-card" id="modeHQ" onclick="setMode('one-to-one')">
          <h3>One-to-One</h3>
          <p>High-Fidelity stereo audio with 24-bit / 96kHz lossless streaming.</p>
        </div>
        <div class="mode-card" id="modeGaming" onclick="setMode('gaming')">
          <h3>Gaming Mode</h3>
          <p>Ultra-low latency audio for fast-paced gaming and real-time talk.</p>
        </div>
        <div class="mode-card" id="modeBroadcast" onclick="setMode('broadcast')">
          <h3>Auracast Broadcast</h3>
          <p>Broadcast high quality audio to multiple nearby Auracast receivers.</p>
        </div>
      </div>
    </div>

    <!-- Details Grid -->
    <div class="details-grid">
      <!-- Codec Selection -->
      <div class="panel-card">
        <div class="section-title">Audio Codecs</div>
        <p style="font-size: 0.85rem; color: var(--text-muted);">Select preferred Bluetooth audio codec:</p>
        <div class="codecs-list" id="codecsList">
          <div class="codec-badge">Loading codecs...</div>
        </div>
      </div>

      <!-- Live Stream Info -->
      <div class="panel-card">
        <div class="section-title">Audio Stream</div>
        <div class="stat-row">
          <span class="stat-label">Active Codec</span>
          <span class="stat-val" id="statCodec">-</span>
        </div>
        <div class="stat-row">
          <span class="stat-label">Sample Rate</span>
          <span class="stat-val" id="statFreq">-</span>
        </div>
        <div class="stat-row">
          <span class="stat-label">Bit Depth</span>
          <span class="stat-val" id="statRes">-</span>
        </div>
        <div class="stat-row">
          <span class="stat-label">Transport</span>
          <span class="stat-val" id="statTransport">-</span>
        </div>
      </div>
    </div>

    <!-- Auracast Panel (shown when broadcast is active or configured) -->
    <div class="bcast-panel" id="bcastPanel">
      <div class="section-title">Auracast™ Broadcast Configuration</div>
      <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
        <div class="input-group">
          <label>Broadcast Name</label>
          <input type="text" id="bcastName" placeholder="Sennheiser BTD 700">
        </div>
        <div class="input-group">
          <label>Audio Quality</label>
          <select id="bcastQuality">
            <option value="hq">High Quality (48 kHz)</option>
            <option value="sq24">Standard Quality (24 kHz)</option>
            <option value="sq16">Standard Quality (16 kHz)</option>
          </select>
        </div>
      </div>
      <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
        <div class="input-group">
          <label>PIN / Password (optional)</label>
          <input type="password" id="bcastKey" placeholder="Leave blank for public broadcast">
        </div>
        <div style="display: flex; align-items: flex-end;">
          <button class="btn btn-primary" onclick="saveBroadcast()">Apply Broadcast Settings</button>
        </div>
      </div>
    </div>

    <!-- Actions -->
    <div class="btn-row">
      <button class="btn btn-secondary" onclick="triggerPairing()">🔄 Bluetooth Pairing</button>
      <button class="btn btn-secondary" onclick="openDfuModal()">⬆️ Firmware Update</button>
      <button class="btn btn-danger" onclick="confirmReset()">⚠️ Factory Reset</button>
    </div>
  </main>

  <!-- Firmware Update Modal -->
  <div class="modal-overlay" id="dfuModal">
    <div class="modal-box">
      <div class="modal-title">Dongle Firmware Update</div>
      <div id="dfuStatus">Checking Sennheiser cloud for updates...</div>
      <div class="notes-box" id="dfuNotes" style="display:none;"></div>
      <div class="btn-row" style="justify-content: flex-end;">
        <button class="btn btn-secondary" onclick="closeDfuModal()">Close</button>
        <button class="btn btn-primary" id="btnUpdate" style="display:none;" onclick="startDfuUpdate()">Update Now</button>
      </div>
    </div>
  </div>

  <footer>
    Sennheiser Dongle Control &bull; Native Linux Port
  </footer>

  <script>
    let pollInterval = null;

    async function fetchStatus() {
      try {
        const res = await fetch('/api/status');
        const data = await res.json();
        updateUI(data);
      } catch (e) {
        document.getElementById('statusBadge').className = 'status-badge disconnected';
        document.getElementById('statusText').innerText = 'Disconnected';
      }
    }

    function updateUI(data) {
      if (!data || !data.connected) {
        document.getElementById('statusBadge').className = 'status-badge disconnected';
        document.getElementById('statusText').innerText = 'No Dongle Found';
        return;
      }

      document.getElementById('statusBadge').className = 'status-badge';
      document.getElementById('statusText').innerText = data.dongle_state || 'Connected';
      document.getElementById('deviceModel').innerText = data.model || 'BTD Dongle';
      if (data.serial) document.getElementById('deviceSerial').innerText = 'Serial: ' + data.serial;

      // Update Mode Cards
      const mode = (data.audio_mode || '').toLowerCase();
      document.getElementById('modeHQ').classList.toggle('active', mode.includes('high') || mode.includes('one'));
      document.getElementById('modeGaming').classList.toggle('active', mode.includes('gaming'));
      document.getElementById('modeBroadcast').classList.toggle('active', mode.includes('broadcast'));

      // Update Stats
      document.getElementById('statCodec').innerText = data.codec_in_use || '-';
      document.getElementById('statFreq').innerText = data.frequency || '-';
      document.getElementById('statRes').innerText = data.resolution || '-';
      document.getElementById('statTransport').innerText = data.connected_transport || data.transport_mode || '-';

      // Update Codecs List
      if (data.supported_codecs) {
        const container = document.getElementById('codecsList');
        container.innerHTML = '';
        data.supported_codecs.forEach(c => {
          const badge = document.createElement('div');
          badge.className = 'codec-badge' + (c.bit === data.codec_in_use_bit ? ' active' : '');
          badge.innerText = c.name;
          badge.onclick = () => setCodec(c.bit);
          container.appendChild(badge);
        });
      }

      // Update Auracast Fields
      if (data.broadcast_name && !document.getElementById('bcastName').matches(':focus')) {
        document.getElementById('bcastName').value = data.broadcast_name;
      }
      if (data.broadcast_key && !document.getElementById('bcastKey').matches(':focus')) {
        document.getElementById('bcastKey').value = data.broadcast_key;
      }
    }

    async function setMode(mode) {
      await fetch('/api/mode', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ mode })
      });
      fetchStatus();
    }

    async function setCodec(bit) {
      await fetch('/api/codec', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ bit })
      });
      fetchStatus();
    }

    async function saveBroadcast() {
      const name = document.getElementById('bcastName').value;
      const quality = document.getElementById('bcastQuality').value;
      const key = document.getElementById('bcastKey').value;
      await fetch('/api/broadcast', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ name, quality, key })
      });
      alert('Broadcast settings saved.');
      fetchStatus();
    }

    async function triggerPairing() {
      await fetch('/api/pair', { method: 'POST' });
      alert('Pairing mode triggered on dongle.');
      fetchStatus();
    }

    async function confirmReset() {
      if (confirm('Are you sure you want to perform a factory reset? All pairings will be erased.')) {
        await fetch('/api/reset', { method: 'POST' });
        alert('Factory reset performed.');
        fetchStatus();
      }
    }

    async function openDfuModal() {
      document.getElementById('dfuModal').classList.add('open');
      document.getElementById('dfuStatus').innerText = 'Querying Sennheiser cloud firmware API...';
      document.getElementById('dfuNotes').style.display = 'none';
      document.getElementById('btnUpdate').style.display = 'none';

      try {
        const res = await fetch('/api/dfu/check');
        const data = await res.json();
        if (data.latest) {
          document.getElementById('dfuStatus').innerHTML = `Latest Available Release: <strong>${data.latest}</strong>`;
          if (data.notes) {
            document.getElementById('dfuNotes').innerText = data.notes;
            document.getElementById('dfuNotes').style.display = 'block';
          }
          document.getElementById('btnUpdate').style.display = 'inline-flex';
        } else {
          document.getElementById('dfuStatus').innerText = 'No new firmware updates found.';
        }
      } catch (e) {
        document.getElementById('dfuStatus').innerText = 'Failed to check updates: ' + e;
      }
    }

    function closeDfuModal() {
      document.getElementById('dfuModal').classList.remove('open');
    }

    async function startDfuUpdate() {
      if (!confirm('Start firmware download and update? Keep dongle plugged in.')) return;
      document.getElementById('dfuStatus').innerText = 'Downloading and validating firmware image...';
      document.getElementById('btnUpdate').style.display = 'none';
      const res = await fetch('/api/dfu/download', { method: 'POST' });
      const data = await res.json();
      if (data.success) {
        document.getElementById('dfuStatus').innerHTML = `<span style="color: var(--success);">Firmware downloaded and validated: ${data.version} (${data.chip}). Ready to flash.</span>`;
      } else {
        document.getElementById('dfuStatus').innerHTML = `<span style="color: var(--danger);">Failed: ${data.error}</span>`;
      }
    }

    fetchStatus();
    pollInterval = setInterval(fetchStatus, 1500);
  </script>
</body>
</html>
"""


class WebRequestHandler(BaseHTTPRequestHandler):
    """Handles REST API and static asset requests."""

    def log_message(self, format, *args):
        pass  # Quiet console logging

    def do_GET(self):
        parsed = urlparse(self.path)
        path = parsed.path

        if path in ("/", "/index.html"):
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.end_headers()
            self.wfile.write(HTML_TEMPLATE.encode("utf-8"))
            return

        elif path.startswith("/assets/"):
            filename = os.path.basename(path)
            asset_path = os.path.join(os.path.dirname(__file__), "assets", filename)
            if os.path.exists(asset_path):
                content_type = "image/png"
                if filename.endswith(".ico"):
                    content_type = "image/x-icon"
                self.send_response(200)
                self.send_header("Content-Type", content_type)
                self.end_headers()
                with open(asset_path, "rb") as f:
                    self.wfile.write(f.read())
                return
            else:
                self.send_response(404)
                self.end_headers()
                return

        elif path == "/api/status":
            dev = get_first_dongle()
            if not dev:
                self._send_json({"connected": False})
                return
            try:
                dev.open()
                status = dev.get_status()
                dev.close()
                self._send_json(status)
            except Exception as e:
                self._send_json({"connected": False, "error": str(e)})
            return

        elif path == "/api/dfu/check":
            client = SennheiserCloudClient()
            dev = get_first_dongle()
            sku = "700434"
            if dev:
                info = dev.dev_info.get("device_info", {})
                sku = info.get("sku", sku)
            try:
                versions = client.get_available_versions(sku)
                latest = versions[0] if versions else None
                notes = client.get_release_notes(sku, latest) if latest else ""
                self._send_json({
                    "latest": latest,
                    "versions": versions,
                    "notes": notes,
                })
            except Exception as e:
                self._send_json({"error": str(e)}, status=500)
            return

        self.send_response(404)
        self.end_headers()

    def do_POST(self):
        parsed = urlparse(self.path)
        path = parsed.path
        body = self._read_json_body()

        dev = get_first_dongle()
        if not dev and path != "/api/dfu/download":
            self._send_json({"error": "No dongle connected"}, status=400)
            return

        if path == "/api/mode":
            mode_str = body.get("mode", "")
            mode_map = {
                "one-to-one": AudioMode.HIGH_QUALITY,
                "gaming": AudioMode.GAMING,
                "broadcast": AudioMode.BROADCAST,
            }
            mode = mode_map.get(mode_str)
            if mode is not None:
                dev.open()
                ok = dev.set_audio_mode(mode)
                dev.close()
                self._send_json({"success": ok})
            else:
                self._send_json({"error": "Invalid mode"}, status=400)
            return

        elif path == "/api/codec":
            bit = body.get("bit")
            if bit is not None:
                dev.open()
                ok = dev.set_codec(int(bit))
                dev.close()
                self._send_json({"success": ok})
            else:
                self._send_json({"error": "Missing bit"}, status=400)
            return

        elif path == "/api/broadcast":
            name = body.get("name")
            quality_str = body.get("quality")
            key = body.get("key")
            qual_map = {
                "sq16": BroadcastQuality.SQ_16K,
                "sq24": BroadcastQuality.SQ_24K,
                "hq": BroadcastQuality.HQ,
            }
            qual = qual_map.get(quality_str)
            dev.open()
            ok = dev.set_broadcast_config(name=name, quality=qual, key=key)
            dev.close()
            self._send_json({"success": ok})
            return

        elif path == "/api/pair":
            dev.open()
            ok = dev.trigger_pairing()
            dev.close()
            self._send_json({"success": ok})
            return

        elif path == "/api/reset":
            dev.open()
            ok = dev.factory_reset()
            dev.close()
            self._send_json({"success": ok})
            return

        elif path == "/api/dfu/download":
            client = SennheiserCloudClient()
            sku = "700434"
            if dev:
                sku = dev.dev_info.get("device_info", {}).get("sku", sku)
            try:
                versions = client.get_available_versions(sku)
                latest = versions[0]
                url = client.get_dfu_download_url(sku, latest)
                dest = f"/tmp/btd_dfu_{latest}.bin"
                client.download_firmware(url, dest)
                from .dfu import DFUImage
                img = DFUImage(dest)
                self._send_json({
                    "success": True,
                    "version": img.firmware_version,
                    "chip": img.chip_model,
                })
            except Exception as e:
                self._send_json({"success": False, "error": str(e)}, status=500)
            return

        self.send_response(404)
        self.end_headers()

    def _read_json_body(self) -> Dict[str, Any]:
        length = int(self.headers.get("content-length", 0))
        if length > 0:
            raw = self.rfile.read(length).decode("utf-8")
            try:
                return json.loads(raw)
            except Exception:
                pass
        return {}

    def _send_json(self, data: Dict[str, Any], status: int = 200):
        body = json.dumps(data).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


def run_server(port: int = 8700, open_browser: bool = True):
    server = HTTPServer(("127.0.0.1", port), WebRequestHandler)
    url = f"http://127.0.0.1:{port}"
    print(f"\033[1;36m━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\033[0m")
    print(f"  \033[1mSennheiser Dongle Control (Linux GUI)\033[0m")
    print(f"  Running at: \033[1;32m{url}\033[0m")
    print(f"  Press Ctrl+C to stop the server.")
    print(f"\033[1;36m━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\033[0m")

    if open_browser:
        threading.Timer(0.5, lambda: webbrowser.open(url)).start()

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopping web server.")
        server.server_close()
