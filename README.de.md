# ◈ NetworkLens

> Portabler Windows-Netzwerkscanner mit Dark Theme — keine Installation erforderlich.

[![Build & Release](https://github.com/C129H223N3O54/NetworkLens/actions/workflows/build.yml/badge.svg)](https://github.com/C129H223N3O54/NetworkLens/actions/workflows/build.yml)
[![Neueste Version](https://img.shields.io/github/v/release/C129H223N3O54/NetworkLens?style=flat&label=Download)](https://github.com/C129H223N3O54/NetworkLens/releases/latest)
[![Lizenz: MIT](https://img.shields.io/badge/Lizenz-MIT-blue.svg)](LICENSE)
[![Plattform](https://img.shields.io/badge/Plattform-Windows%2010%2F11-informational)](https://github.com/C129H223N3O54/NetworkLens/releases/latest)

---

## ✨ Features

| Feature | Beschreibung |
|---|---|
| 🔍 **Netzwerk-Scan** | Paralleler Ping-Sweep mit Live-Ergebnissen, DNS Reverse-Lookup, MAC per ARP, Hersteller-Erkennung |
| 🔌 **Port-Scanner** | Quick / Standard / Full / Custom Profile, Banner-Grabbing, ~60 bekannte Dienste |
| 📊 **Netzwerk-Info** | Lokale IP, Gateway, DNS, öffentliche IP, WLAN-SSID, Adapter-Liste, aktive Verbindungen (netstat) |
| 📡 **Live-Monitor** | Kontinuierliche Ping-Überwachung mit Live Ping + Jitter Graph, Packet Loss Zähler |
| 🏷️ **Geräte-Verwaltung** | Alias, Kategorie, Favoriten — scan-übergreifend per MAC gespeichert |
| 🔄 **Scan-Vergleich** | Neue, verschwundene und geänderte Geräte zwischen Scans erkennen |
| 📄 **Reports** | Interaktiver HTML-Report, CSV und JSON Export |
| 🖥️ **Kontextmenü** | Öffnen per HTTP/HTTPS/FTP/SSH/Telnet, Ping, Traceroute, GeoLocate, IP/MAC kopieren |

---

## 🚀 Schnellstart

1. `NetworkLens.exe` aus dem [neuesten Release](https://github.com/C129H223N3O54/NetworkLens/releases/latest) herunterladen
2. Direkt starten — **kein .NET erforderlich**
3. Für alle Funktionen (MAC-Adressen, ARP): Rechtsklick → **Als Administrator ausführen**

> **Windows Smart App Control:** Falls Windows die App blockiert, Rechtsklick auf die exe → Eigenschaften → **"Zulassen"** anklicken → OK

---

## 🔒 Admin vs. Kein Admin

| Funktion | Ohne Admin | Mit Admin |
|---|---|---|
| Ping-Sweep | ✅ | ✅ |
| DNS Reverse-Lookup | ✅ | ✅ |
| Netzwerk-Adapter Info | ✅ | ✅ |
| MAC-Adressen (ARP) | ⚠️ Nur eigenes Subnetz | ✅ Alle Geräte |
| Port-Scanner | ✅ | ✅ |
| Netstat mit Prozessnamen | ❌ | ✅ |

---

## 📡 Live-Monitor

Der Monitor überwacht jedes Gerät kontinuierlich:

- **Ping** — Antwortzeit in ms (Round-Trip Time)
- **Jitter** — Schwankung des Pings über Zeit (zeigt Verbindungsstabilität)
- **Packet Loss** — Prozentsatz und absoluter Zähler verlorener Pakete
- **Graph** — Live Ping (Cyan) + Jitter (Gelb gestrichelt) — Doppelklick öffnet Vollbild-Fenster
- **Intervalle** — 1s / 5s / 10s / 30s / 60s / Dauerhaft

---

## 🛠️ Aus dem Quellcode bauen

**Voraussetzungen:**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Windows (WPF erfordert Windows)

```bash
git clone https://github.com/C129H223N3O54/NetworkLens.git
cd NetworkLens
dotnet run --project NetworkLens/NetworkLens.csproj
```

**Single-File EXE bauen:**
```powershell
dotnet publish NetworkLens/NetworkLens.csproj `
  -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish
```

**Release per Tag auslösen:**
```bash
git tag v1.0.0
git push origin v1.0.0
```

---

## 📁 Datenspeicherung

Alle Daten werden in `%APPDATA%\NetworkLens\` gespeichert:

| Datei | Zweck |
|---|---|
| `config.json` | App-Einstellungen |
| `devices.json` | Geräte-Aliase, Kategorien, Favoriten |
| `scans\*.json` | Scan-Verlauf als JSON-Snapshots |

---

## 👤 Credits

| Rolle | Person |
|---|---|
| 💡 Idee & Konzept | Jan Erik Mueller |
| 🤖 Entwicklung | Claude (Anthropic) |

---

## 📜 Lizenz

MIT License — siehe [LICENSE](LICENSE)

---

*Entwickelt mit ❤️ und vielen PowerShell-Skripten*
