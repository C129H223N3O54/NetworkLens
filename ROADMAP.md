# NetworkLens — Roadmap

This document tracks planned features and improvements for future versions.
For released versions, see the in-app changelog (Settings → Changelog).

---

## v1.4.0 — Bulk Port-Scan

**Status:** Planned
**Priority:** High
**Estimated effort:** ~1 week

### Summary

Add the ability to scan ports across multiple network devices at once,
with flexible device selection, custom port configuration, and rich
result visualization.

### User stories

- As a user, I want to scan a chosen set of ports across **all devices**
  found in my network so that I can quickly audit which services are
  exposed across my LAN.
- As a user, I want to **filter** which devices are included
  (by category, favorites, subnet, online-only) so I can focus on what
  matters.
- As a user, I want to choose between **predefined profiles**
  (Quick / Standard / Full) or **custom port lists** to balance speed
  and coverage.
- As a user, I want to see results in **multiple views**
  (live table, heatmap matrix, HTML report) depending on what I'm
  looking for.
- As a user, I want bulk scans to **persist** alongside regular scans
  so I can review them later.

### UI design

#### New tab: "Bulk Port-Scan"

Position: between "Port-Scanner" and "Live-Monitor" in the main tab bar.

#### Layout

```
┌────────────────────────────────────────────────────────┐
│  Bulk Port-Scan                                        │
├──────────────────────────┬─────────────────────────────┤
│ Device selection         │ Port configuration          │
│ ┌──────────────────────┐ │  Profile: [Standard ▾]      │
│ │ Categories [▾]       │ │  ○ Profile  ● Custom        │
│ │ ☐ Favorites only     │ │  Ports: [80,443,22,3389  ]  │
│ │ Subnet [▾]           │ │                             │
│ │ ☑ Online only        │ │  Concurrency: [10] devices  │
│ └──────────────────────┘ │  Timeout: [500] ms          │
│                          │                             │
│ Selected: 24/47 devices  │  [▶ Start scan]             │
│                          │  [⏹ Cancel]                 │
├──────────────────────────┴─────────────────────────────┤
│ [Table] [Heatmap] [HTML report]                        │
│                                                        │
│ ▸ View content                                         │
└────────────────────────────────────────────────────────┘
```

### Result views

1. **Live table** — one row per device with columns: IP, Hostname,
   Status (Pending / Scanning / Done / Failed), open-port count,
   list of open ports, total scan time. Updates live during scan.

2. **Heatmap matrix** — devices on Y-axis, ports on X-axis, color-coded:
   - 🟢 Green = open
   - 🟡 Yellow = filtered
   - ⬛ Dark = closed
   - ⬜ Light gray = not yet scanned
   - Click cell for details popup
   - Recommended for ≤ 50 devices × ≤ 50 ports; for "Full" profile
     the heatmap is hidden as it becomes unreadable.

3. **HTML report** — reuses the existing `ReportGenerator` pattern.
   Embeds heatmap as SVG, includes detail table, statistics
   (top-10 most-exposed devices, most-common open ports), and the
   same filter / theme-toggle as the standard report.

### Scan-View integration

A new toolbar button in the existing Scan view:
**"🔓 Ports auf allen scannen…"** / **"🔓 Scan ports on all…"**

Clicking it opens the new Bulk Port-Scan tab with the current device
list pre-loaded. Filters can then be refined in the new tab.

### Technical details

- **Concurrency limit**: `SemaphoreSlim` with default 10 parallel
  devices (configurable 1-50). Prevents network flooding and avoids
  triggering IDS / DDoS-detection heuristics on consumer routers.
- **Per-device cancellation**: unreachable devices fail fast rather
  than blocking the queue.
- **Persistence**: results saved to `scans/<timestamp>_bulkports/`,
  selectable in the Report tab as an additional comparison source.
- **Bilingual from day one**: every new string registered as a
  translation key in `Strings.cs`.

### New translation keys (provisional)

```
Bulk_TabTitle              "Bulk Port-Scan"
Bulk_DeviceSelection       "Geräte-Auswahl" / "Device selection"
Bulk_PortConfig            "Port-Konfiguration" / "Port configuration"
Bulk_FilterCategories      "Kategorien" / "Categories"
Bulk_FilterFavorites       "Nur Favoriten" / "Favorites only"
Bulk_FilterSubnet          "Subnetz" / "Subnet"
Bulk_FilterOnline          "Nur Online" / "Online only"
Bulk_SelectionCountFmt     "Auswahl: {0}/{1} Geräte" / "Selected: {0}/{1} devices"
Bulk_Concurrency           "Parallelität" / "Concurrency"
Bulk_Timeout               "Timeout" / "Timeout"
Bulk_StartScan             "Scan starten" / "Start scan"
Bulk_Cancel                "Abbrechen" / "Cancel"
Bulk_ViewTable             "Tabelle" / "Table"
Bulk_ViewHeatmap           "Heatmap" / "Heatmap"
Bulk_ViewReport            "HTML-Report" / "HTML report"
Bulk_StatusPending         "Wartend" / "Pending"
Bulk_StatusScanning        "Scannt..." / "Scanning..."
Bulk_StatusDone            "Fertig" / "Done"
Bulk_StatusFailed          "Fehlgeschlagen" / "Failed"
Bulk_ColOpenPorts          "Offene Ports" / "Open ports"
Bulk_ColScanTime           "Scan-Zeit" / "Scan time"
Bulk_HeatmapLegendOpen     "Offen" / "Open"
Bulk_HeatmapLegendFiltered "Gefiltert" / "Filtered"
Bulk_HeatmapLegendClosed   "Geschlossen" / "Closed"
Bulk_HeatmapLegendUnscanned "Nicht gescannt" / "Not scanned"
Bulk_StatsTopExposed       "Top exponierte Geräte" / "Top exposed devices"
Bulk_StatsCommonPorts      "Häufigste offene Ports" / "Most common open ports"
ScanView_BulkScanButton    "Ports auf allen scannen…" / "Scan ports on all…"
```

### Files to add / modify (estimate)

**New files (~1100 lines):**
- `Views/BulkScanView.xaml` — main tab UI
- `Views/BulkScanView.xaml.cs` — code-behind
- `ViewModels/BulkScanViewModel.cs` — orchestration
- `Models/BulkScanResult.cs` — per-device result
- `Models/BulkScanSession.cs` — session aggregate
- `Services/BulkScanner.cs` — runs the parallelized scans
- `Helpers/HeatmapRenderer.cs` — WPF Canvas-based heatmap
- `Helpers/BulkReportGenerator.cs` — HTML report generation

**Modified files (~250 lines added):**
- `MainWindow.xaml` — new tab
- `Views/ScanView.xaml` + `.xaml.cs` — bulk-scan button
- `Localization/Strings.cs` — ~30 new keys
- `NetworkLens.csproj` — version bump
- `App.xaml.cs` — build timestamp

### Acceptance criteria

- [ ] New tab appears between Port-Scanner and Live-Monitor
- [ ] Device list filters work and combine correctly (AND-logic)
- [ ] Selection count updates live as filters change
- [ ] Custom port string parses ranges (`8000-8100`) and lists (`80,443`)
- [ ] Concurrency limit is respected (verified via parallel-task logging)
- [ ] Cancellation aborts cleanly within 1 second
- [ ] Live table updates as devices complete
- [ ] Heatmap renders correctly for up to 100 devices × 100 ports
- [ ] HTML report opens in default browser, looks correct in light + dark
- [ ] All UI text switches between DE / EN without restart
- [ ] Bulk scan results persist and appear in Report tab dropdown
- [ ] No hardcoded German or English strings introduced

### Risks and open questions

- **Heatmap performance** with large port ranges — fallback: only
  show heatmap if `ports.Length ≤ 256`, otherwise show table-only
  with a hint.
- **Network impact** at high concurrency — needs real-world testing
  on home networks; default may need to be lower than 10.
- **IDS / firewall reactions** on enterprise networks — the in-app
  hint should warn that bulk scans look like reconnaissance to
  intrusion-detection systems.

---

## Future ideas (unscoped)

These are noted for later consideration; no design work yet.

- **Scheduled scans**: run a network or bulk port scan on a cron-like
  schedule, with email or Toast notification on diff.
- **Service fingerprinting** beyond banner-grabbing
  (e.g., HTTP headers, SSH banners parsed for version).
- **Vulnerability hints**: known-bad-version warnings for detected
  services (offline CVE database).
- **Multi-subnet scanning**: scan several subnets sequentially in
  one session.
- **Wake-on-LAN integration**: wake known devices by MAC from the
  device list.
- **API mode**: optional local HTTP API to trigger scans from
  scripts / Home Assistant.
- **Linux / macOS port** via Avalonia or .NET MAUI (large effort,
  separate project).
