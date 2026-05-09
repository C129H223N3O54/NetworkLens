# Bulk Port-Scan across all discovered devices

## Summary

Add the ability to scan a chosen set of ports across multiple network
devices at once, with flexible device filtering, custom port
configuration, and rich result visualization (live table, heatmap,
HTML report).

## Motivation

Currently the Port-Scanner tab works on a single IP at a time. To
audit which services are exposed across the LAN, the user has to
scan each device individually — tedious and error-prone. A bulk-scan
mode would let the user answer questions like:

- Which devices on my network expose SSH (port 22)?
- Are any of my IoT devices listening on Telnet (port 23)?
- What does my "attack surface" look like across the whole subnet?

## Proposed UI

### New tab: "Bulk Port-Scan" (between Port-Scanner and Live-Monitor)

Layout split into two panels above and three result views below:

**Top — Device selection (left) + Port config (right):**
- **Filters:** Categories (multi-select), Favorites only, Subnet, Online only
- **Selection count** updates live (`Selected: 24/47 devices`)
- **Profile selector:** Quick / Standard / Full / Custom (matches PortScan tab)
- **Custom port input** with same syntax (`80,443,8000-8100`)
- **Concurrency** slider (default 10, range 1-50)
- **Per-device timeout** (default 500 ms)
- **Start / Cancel** buttons

**Bottom — Result views (tab-switchable):**

1. **Live table** — row per device: IP, Hostname, Status,
   open-port count, port list, scan time. Updates during scan.

2. **Heatmap matrix** — devices × ports as colored grid:
   green = open, yellow = filtered, dark = closed,
   light gray = unscanned. Click cell for details.
   Hidden when port count > 256 (becomes unreadable).

3. **HTML report** — reuses existing `ReportGenerator` style,
   embeds heatmap as SVG, includes top-10 most-exposed devices
   and most-common open ports stats.

### Scan-View integration

New toolbar button **"🔓 Ports auf allen scannen…"** in the existing
Scan view. Opens the new tab with the current device list pre-loaded.

## Technical notes

- `SemaphoreSlim` for concurrency control
- Per-device cancellation token, fail-fast on unreachable devices
- Results persist to `scans/<timestamp>_bulkports/`, selectable in Report tab
- All new strings as translation keys (DE / EN) from day one

## Files (estimate)

**New (~1100 lines):** `BulkScanView.xaml` + `.xaml.cs`,
`BulkScanViewModel.cs`, `BulkScanResult.cs`, `BulkScanSession.cs`,
`BulkScanner.cs`, `HeatmapRenderer.cs`, `BulkReportGenerator.cs`.

**Modified (~250 lines added):** `MainWindow.xaml`, `ScanView.xaml(.cs)`,
`Strings.cs` (~30 new keys), `.csproj`, `App.xaml.cs`.

## Acceptance criteria

- [ ] New tab visible between Port-Scanner and Live-Monitor
- [ ] All filters combine correctly (AND-logic) and update count live
- [ ] Custom port string parses ranges and lists
- [ ] Concurrency limit respected
- [ ] Cancellation aborts within 1 second
- [ ] Live table updates as devices complete
- [ ] Heatmap renders for up to 100 devices × 100 ports
- [ ] HTML report opens correctly in light + dark theme
- [ ] All UI text switches DE / EN without restart
- [ ] Results persist and appear as comparable scan in Report tab
- [ ] No hardcoded German or English strings introduced

## Risks

- Heatmap performance with large port ranges → cap at 256 ports
- Network impact at high concurrency → may need lower default
- IDS / firewall reactions → in-app hint about reconnaissance signature

## Target version

v1.4.0

## Effort

~1 week

---

*See `ROADMAP.md` for the full design document.*
