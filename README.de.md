# ◈ NetworkLens

> Portabler, selbständiger Windows-Netzwerkscanner — keine Installation nötig.

[![Build & Release](https://github.com/YOUR_USERNAME/NetworkLens/actions/workflows/build.yml/badge.svg)](https://github.com/YOUR_USERNAME/NetworkLens/actions/workflows/build.yml)
[![Neueste Version](https://img.shields.io/github/v/release/YOUR_USERNAME/NetworkLens?style=flat)](https://github.com/YOUR_USERNAME/NetworkLens/releases/latest)

## ✨ Features

| Feature | Beschreibung |
|---|---|
| 🔍 **Netzwerk-Scan** | Paralleler Ping-Sweep, DNS Reverse-Lookup, MAC per ARP |
| 🔌 **Port-Scanner** | Quick / Standard / Full / Custom, Banner-Grabbing |
| 📊 **Netzwerk-Info** | Adapter, IP, Gateway, DNS, öffentliche IP, WLAN-SSID |
| 📡 **Live-Monitor** | Ping-Überwachung, Jitter, Packet Loss, Sparkline-Graph |
| 🏷️ **Geräte-Verwaltung** | Alias, Kategorie, Favoriten, Notizen — scan-übergreifend gespeichert |
| 🔄 **Scan-Vergleich** | Neue / verschwundene / geänderte Geräte zwischen Scans erkennen |
| 📄 **Reports** | Interaktiver HTML-Report, CSV, JSON-Export |
| 🔔 **Alerts** | Toast-Benachrichtigungen bei neuen Geräten, Offline-Events, neuen Ports |

## 🚀 Schnellstart

1. `NetworkLens.exe` aus dem [neuesten Release](https://github.com/YOUR_USERNAME/NetworkLens/releases/latest) herunterladen
2. Direkt starten — **kein .NET-Install nötig**
3. Für alle Funktionen (MAC-Adressen, ARP-Cache): als Administrator starten

## 🔒 Admin vs. Kein Admin

| Funktion | Ohne Admin | Mit Admin |
|---|---|---|
| Ping-Sweep | ✅ | ✅ |
| DNS Reverse-Lookup | ✅ | ✅ |
| Netzwerk-Adapter Info | ✅ | ✅ |
| MAC-Adressen (ARP) | ⚠️ Nur eigenes Subnetz | ✅ Alle Geräte |
| Port-Scanner | ✅ | ✅ |
| Netstat mit Prozessnamen | ❌ | ✅ |

## 🛠️ Aus dem Quellcode bauen

### Voraussetzungen
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Windows (WPF erfordert Windows)

```bash
git clone https://github.com/YOUR_USERNAME/NetworkLens.git
cd NetworkLens
dotnet run --project NetworkLens/NetworkLens.csproj
```

### Single-File EXE bauen
```bash
dotnet publish NetworkLens/NetworkLens.csproj `
  -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish
```

### GitHub Actions Release
Ein Versions-Tag pushen für automatischen Build und Release:
```bash
git tag v1.0.0
git push origin v1.0.0
```

## 📁 Datenspeicherung

| Datei | Zweck |
|---|---|
| `%APPDATA%\NetworkLens\config.json` | Einstellungen |
| `%APPDATA%\NetworkLens\devices.json` | Geräte-Aliase, Kategorien, Favoriten |
| `%APPDATA%\NetworkLens\scans\*.json` | Scan-Verlauf als JSON-Snapshots |

## 🎨 OUI-Datenbank (MAC-Hersteller)

Die App enthält eine minimale eingebettete OUI-Datenbank. Für die vollständige Datenbank (~30.000 Einträge):

1. Von [IEEE OUI Registry](https://standards-oui.ieee.org/oui/oui.txt) herunterladen
2. Als `NetworkLens/Resources/oui.txt` speichern
3. Neu kompilieren — wird automatisch eingebettet

## 📜 Lizenz

MIT License — siehe [LICENSE](LICENSE)
