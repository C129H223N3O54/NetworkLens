# ◈ NetworkLens — Roadmap

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

---

## 🔜 Planned

### v1.3.0 — Report & Export
- [ ] Theme toggle button inside HTML report (Light/Dark in browser)
- [ ] Embed current app theme into exported report
- [ ] PDF export
- [ ] Auto-open report after export

### v1.4.0 — Scan Improvements
- [ ] Save scan profiles (custom subnet/timeout combinations)
- [ ] Scan multiple subnets simultaneously
- [ ] Device history — track when a device was last seen online
- [ ] Wake-on-LAN — wake devices via Magic Packet

### v1.5.0 — Monitor Improvements
- [ ] Compare multiple devices in one graph simultaneously
- [ ] Export graph as PNG/SVG
- [ ] Threshold alerts — notify when ping exceeds X ms
- [ ] Save monitor history across sessions

### v1.6.0 — Network Info Enhancements
- [ ] DHCP lease viewer — see which IPs are assigned
- [ ] Simple network topology visualization
- [ ] Basic bandwidth test to gateway

### v2.0.0 — Major Features
- [ ] SQLite database instead of JSON (better performance at scale)
- [ ] Plugin system — integrate custom scanners/reporters
- [ ] Local REST API — query NetworkLens programmatically
- [ ] Additional languages (EN/DE already built-in)

---

## 💡 Quick Wins (Small but useful)
- [ ] Show device count badge in sidebar
- [ ] Favorites pinned to top of scan results
- [ ] Auto-load last scan on startup
- [ ] Double-click device to open full details panel

---

## 💬 Feedback & Ideas

Have a feature request or found a bug?
Open an issue at [github.com/C129H223N3O54/NetworkLens](https://github.com/C129H223N3O54/NetworkLens/issues)

---

*Made with ❤️ by Jan Erik Mueller & Claude (Anthropic)*
