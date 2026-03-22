# ◈ NetworkLens

> A portable, self-contained Windows network scanner — no installation required.

![NetworkLens Screenshot](screenshot.png)

[![Build & Release](https://github.com/YOUR_USERNAME/NetworkLens/actions/workflows/build.yml/badge.svg)](https://github.com/YOUR_USERNAME/NetworkLens/actions/workflows/build.yml)
[![Latest Release](https://img.shields.io/github/v/release/YOUR_USERNAME/NetworkLens?style=flat)](https://github.com/YOUR_USERNAME/NetworkLens/releases/latest)

## ✨ Features

| Feature | Description |
|---|---|
| 🔍 **Network Scan** | Parallel ping sweep, DNS reverse lookup, ARP MAC detection |
| 🔌 **Port Scanner** | Quick / Standard / Full / Custom profiles, banner grabbing |
| 📊 **Network Info** | Adapters, IPs, gateway, DNS, public IP, SSID, active connections |
| 📡 **Live Monitor** | Continuous ping, jitter, packet loss, sparkline history |
| 🏷️ **Device Management** | Aliases, categories, favorites, notes — persisted across scans |
| 🔄 **Scan Comparison** | Detect new / disappeared / changed devices between scans |
| 📄 **Reports** | Interactive HTML report, CSV, JSON export |
| 🔔 **Alerts** | Toast notifications for new devices, offline events, new ports |

## 🚀 Quick Start

1. Download `NetworkLens.exe` from the [latest release](https://github.com/YOUR_USERNAME/NetworkLens/releases/latest)
2. Run it — **no .NET installation required**
3. For full functionality (MAC addresses, ARP cache, process names): run as Administrator

## 🔒 Admin vs. Non-Admin

| Feature | Without Admin | With Admin |
|---|---|---|
| Ping sweep | ✅ | ✅ |
| DNS reverse lookup | ✅ | ✅ |
| Network adapter info | ✅ | ✅ |
| MAC addresses (ARP) | ⚠️ Own subnet only | ✅ All devices |
| Port scanner | ✅ | ✅ |
| Netstat with process names | ❌ | ✅ |

## 🛠️ Building from Source

### Requirements
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Windows (WPF requires Windows)

```bash
git clone https://github.com/YOUR_USERNAME/NetworkLens.git
cd NetworkLens
dotnet run --project NetworkLens/NetworkLens.csproj
```

### Build single-file exe
```bash
dotnet publish NetworkLens/NetworkLens.csproj \
  -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o ./publish
```

### GitHub Actions Release
Push a version tag to trigger an automatic build and release:
```bash
git tag v1.0.0
git push origin v1.0.0
```

## 📁 Data Storage

| File | Purpose |
|---|---|
| `%APPDATA%\NetworkLens\config.json` | Settings |
| `%APPDATA%\NetworkLens\devices.json` | Device aliases, categories, favorites |
| `%APPDATA%\NetworkLens\scans\*.json` | Scan history snapshots |

## 🎨 OUI Database (MAC Manufacturer Lookup)

The app ships with a minimal built-in OUI database. For the full database (~30,000 entries):

1. Download from [IEEE OUI Registry](https://standards-oui.ieee.org/oui/oui.txt)
2. Place as `NetworkLens/Resources/oui.txt`
3. Rebuild — it will be embedded in the exe

## 📜 License

MIT License — see [LICENSE](LICENSE)
