# ◈ NetworkLens

> Portable, self-contained Windows network scanner with Light & Dark theme — no installation required.

[![Build & Release](https://github.com/C129H223N3O54/NetworkLens/actions/workflows/build.yml/badge.svg)](https://github.com/C129H223N3O54/NetworkLens/actions/workflows/build.yml)
[![Latest Release](https://img.shields.io/github/v/release/C129H223N3O54/NetworkLens?style=flat&label=Download)](https://github.com/C129H223N3O54/NetworkLens/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11%2FServer-informational)](https://github.com/C129H223N3O54/NetworkLens/releases/latest)

---

## ✨ Features

| Feature | Description |
|---|---|
| 🔍 **Network Scan** | Parallel ping sweep with live results, DNS reverse lookup, MAC via ARP, manufacturer detection |
| 🔌 **Port Scanner** | Quick / Standard / Full / Custom profiles, banner grabbing, ~60 known services |
| 📊 **Network Info** | Local IP, gateway, DNS, public IP, WLAN SSID, adapter list, active connections (netstat) |
| 📡 **Live Monitor** | Continuous ping monitoring with live Ping + Jitter graph, Packet Loss counter |
| 🏷️ **Device Management** | Aliases, categories, favorites — persisted per MAC across scans |
| 🔄 **Scan Comparison** | Detect new, disappeared and changed devices between scans |
| 📄 **Reports** | Interactive HTML report, CSV and JSON export |
| 🖥️ **Context Menu** | Open via HTTP/HTTPS/FTP/SSH/Telnet, Ping, Traceroute, GeoLocate, copy IP/MAC |
| 🎨 **Light / Dark Theme** | Toggle in Settings — switches instantly, preference saved across sessions |

---

## 🚀 Quick Start

1. Download `NetworkLens.exe` from the [latest release](https://github.com/C129H223N3O54/NetworkLens/releases/latest)
2. Run it — **no .NET installation required**
3. For full functionality (MAC addresses, ARP): right-click → **Run as Administrator**

> **Windows Server:** Desktop Experience must be installed (Server Core without GUI is not supported).

> **Windows Smart App Control:** If Windows blocks the app, right-click the exe → Properties → check **"Unblock"** → OK

---

## 🖥️ Screenshots

| Network Scan | Live Monitor |
|---|---|
| *Live results as devices respond* | *Ping + Jitter graph with double-click fullscreen* |

---

## 🔒 Admin vs. Non-Admin

| Feature | Without Admin | With Admin |
|---|---|---|
| Ping sweep | ✅ | ✅ |
| DNS reverse lookup | ✅ | ✅ |
| Network adapter info | ✅ | ✅ |
| MAC addresses (ARP) | ⚠️ Own subnet only | ✅ All devices |
| Port scanner | ✅ | ✅ |
| Netstat with process names | ❌ | ✅ |

---

## 📡 Live Monitor

The monitor tracks each device continuously:

- **Ping** — Round-trip time in ms
- **Jitter** — Variation in ping over time (indicates connection stability)
- **Packet Loss** — Percentage and absolute count of lost packets
- **Graph** — Live Ping (cyan) + Jitter (yellow dashed) — double-click to open fullscreen window
- **Intervals** — 1s / 5s / 10s / 30s / 60s / Continuous

---

## 🛠️ Building from Source

**Requirements:**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Windows (WPF requires Windows)

```bash
git clone https://github.com/C129H223N3O54/NetworkLens.git
cd NetworkLens
dotnet run --project NetworkLens/NetworkLens.csproj
```

**Build single-file exe:**
```powershell
dotnet publish NetworkLens/NetworkLens.csproj `
  -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish
```

**Trigger a release via tag:**
```bash
git tag v1.1.0
git push origin v1.1.0
```

---

## 📁 Data Storage

All data is stored in `%APPDATA%\NetworkLens\`:

| File | Purpose |
|---|---|
| `config.json` | App settings and preferences |
| `theme.json` | Light / Dark theme preference |
| `devices.json` | Device aliases, categories, favorites |
| `scans\*.json` | Scan history snapshots |

---

## 👤 Credits

| Role | Person |
|---|---|
| 💡 Idea & Concept | Jan Erik Mueller |
| 🤖 Development | Claude (Anthropic) |

---

## 📜 License

MIT License — see [LICENSE](LICENSE)

---

## 🗺️ Roadmap

See [ROADMAP.md](ROADMAP.md) for planned features and upcoming releases.

---

*Made with ❤️ by Jan Erik Mueller & Claude (Anthropic)*
