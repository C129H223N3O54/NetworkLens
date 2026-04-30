# Sideforge / NetworkLens — Roadmap

[![Sideforge](https://img.shields.io/badge/Sideforge-Design_System-E8600A?style=flat)](https://github.com/C129H223N3O54/SideForge)

> This roadmap outlines planned features and improvements. Order and scope may change based on feedback.

---

## ✅ Released

### v1.0.0 — Initial Release
- Network scan with live results, DNS, MAC, manufacturer detection
- Port scanner: Quick / Standard / Full / Custom profiles, banner grabbing
- Network info: IP, gateway, DNS, WLAN SSID, adapters, netstat
- Live monitor: Ping/Jitter graph, Packet Loss counter, double-click fullscreen
- Device management: Alias, category, favorites (persistent per MAC)
- Reports: HTML (interactive), CSV, JSON, scan comparison
- Context menu: HTTP/HTTPS/FTP/SSH/Telnet/Traceroute/GeoLocate
- Self-contained .exe, MIT License

### v1.1.0 — Light / Dark Theme
- Light Mode and Dark Mode toggle in Settings
- Theme switches instantly without restart
- Theme preference saved and restored on next launch
- Context menu, monitor, graph window follow theme
- HTML report respects system theme automatically

### v1.2.0 — Sideforge Design System
- Migration to the [Sideforge](https://github.com/C129H223N3O54/SideForge) design language
- Ember-orange primary accent (replaces Cyan)
- Warm Anvil grayscale (replaces cool blue-gray)
- SF-Logo + Sideforge wordmark in app header
- Verdana for UI, Georgia italic for logo, Consolas for monospace
- Light + Dark mode recalibrated for the Sideforge palette

### v1.2.1 — Polish & Bugfixes
- HTML report now follows the app's current theme
- Theme toggle button inside HTML report
- Filter clear button (×) in HTML report
- Ember-orange scrollbars in WPF and HTML
- Cleaned context menu (consistent alignment)

### v1.3.0 — Internationalization (DE / EN)
- Full multilingual support: German and English
- Auto-detect Windows system language on first start
- Live switch without restart
- Wikipedia links language-aware (de / en)
- Language preference saved (`language.json`)

### v1.3.1 — Translation Coverage + Bugfixes
- 90+ additional translations across all main views
- Column headers, buttons, hint texts, notification labels
- Fix: 'ABHÖREN' encoding bug for active connections (CP850 OEM)
- Fix: Connection state language-aware (LISTEN, ESTABLISHED, ...)
- Fix: Report compare dropdowns show date + subnet instead of class name
- Footer "devices / Geräte" follows the language

### v1.3.2 — Full Translation Coverage
- All dialogs translated (Alias, Category, Graph window)
- All MessageBoxes (errors, confirmations, hints)
- Status texts, toast notifications, tooltips
- 280+ translation keys in `Strings.cs`

### v1.3.3 — Final Translation Polish
- Category dialog fully localized
- Admin hint panel localized
- Monitor AVG tooltip and ScanView progress text
- Report buttons and comparison status messages
- 290 translation keys

### v1.3.4 — Changelog Translation
- All historic changelog entries translated to English
- Consistent English wording across all release notes

---

## 🔜 Planned

### v1.4.0 — Monitor Recording
- [ ] Record continuous monitoring sessions to disk
- [ ] Filter recordings by time range, device, threshold
- [ ] Export recording as CSV/JSON for further analysis
- [ ] Replay / scrub through recorded data
- [ ] Long-term statistics across multiple sessions

### v1.5.0 — Report & Export
- [ ] PDF export
- [ ] Auto-open report after export
- [ ] Custom report templates
- [ ] Scheduled / recurring reports

### v1.6.0 — Scan Improvements
- [ ] Save scan profiles (custom subnet/timeout combinations)
- [ ] Scan multiple subnets simultaneously
- [ ] Device history — track when a device was last seen online
- [ ] Wake-on-LAN — wake devices via Magic Packet

### v1.7.0 — Monitor Improvements
- [ ] Compare multiple devices in one graph simultaneously
- [ ] Export graph as PNG/SVG
- [ ] Threshold alerts — notify when ping exceeds X ms
- [ ] Save monitor history across sessions

### v1.8.0 — Network Info Enhancements
- [ ] DHCP lease viewer — see which IPs are assigned
- [ ] Simple network topology visualization
- [ ] Basic bandwidth test to gateway

### v2.0.0 — Major Features
- [ ] SQLite database instead of JSON (better performance at scale)
- [ ] Plugin system — integrate custom scanners/reporters
- [ ] Local REST API — query NetworkLens programmatically
- [ ] Additional languages via resource files (FR, ES, IT, ...)

---

## 💡 Quick Wins (Small but useful)
- [ ] Show device count badge in sidebar
- [ ] Favorites pinned to top of scan results
- [ ] Auto-load last scan on startup
- [ ] Double-click device to open full details panel
- [ ] Keyboard shortcuts (Ctrl+R for rescan, Ctrl+E for export, ...)
- [ ] Tray icon with quick actions

---

## 💬 Feedback & Ideas

Have a feature request or found a bug?
Open an issue at [github.com/C129H223N3O54/NetworkLens](https://github.com/C129H223N3O54/NetworkLens/issues)

---

*Made with ❤️ by Jan Erik Mueller & Claude (Anthropic)*
