using System.Text;
using System.Text.Json;
using NetworkLens.Models;

namespace NetworkLens.Services;

public class ReportGenerator
{
    public async Task<string> GenerateHtmlAsync(ScanResult scan, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BuildHtml(scan, App.IsDarkTheme ? "dark" : "light"));

        var filePath = Path.Combine(outputPath, $"NetworkLens_Report_{scan.FilenameSlug}.html");
        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    public async Task<string> GenerateCsvAsync(ScanResult scan, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("IP-Adresse,Hostname,Alias,MAC-Adresse,Hersteller,Status,Ping (ms),Offene Ports,Kategorie,Zuletzt gesehen");

        foreach (var d in scan.Devices.OrderBy(d => d.IpSort))
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(d.IpAddress),
                CsvEscape(d.Hostname),
                CsvEscape(d.Alias),
                CsvEscape(d.MacAddress),
                CsvEscape(d.Manufacturer),
                d.Status.ToString(),
                d.ResponseTime >= 0 ? d.ResponseTime.ToString() : "",
                d.OpenPortCount.ToString(),
                d.Category.ToString(),
                d.LastSeen.ToString("dd.MM.yyyy HH:mm:ss")
            ));
        }

        var filePath = Path.Combine(outputPath, $"NetworkLens_Export_{scan.FilenameSlug}.csv");
        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    public async Task<string> GenerateJsonAsync(ScanResult scan, string outputPath)
    {
        var json = JsonSerializer.Serialize(scan, new JsonSerializerOptions { WriteIndented = true });
        var filePath = Path.Combine(outputPath, $"NetworkLens_Scan_{scan.FilenameSlug}.json");
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
        return filePath;
    }

    private static string CsvEscape(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    private static string BuildHtml(ScanResult scan, string initialTheme)
    {
        var online = scan.Devices.Count(d => d.Status is DeviceStatus.Online or DeviceStatus.Slow);
        var offline = scan.Devices.Count(d => d.Status == DeviceStatus.Offline);
        var newDev = scan.Devices.Count(d => d.IsNew);

        var rows = new StringBuilder();
        foreach (var d in scan.Devices.OrderBy(d => d.IpSort))
        {
            var statusColor = d.Status switch
            {
                DeviceStatus.Online  => "#74A732",
                DeviceStatus.Slow    => "#BA7517",
                DeviceStatus.Offline => "#DC2626",
                _                    => "#888780"
            };
            var portsHtml = d.OpenPorts.Count > 0
                ? string.Join(", ", d.OpenPorts.Where(p => p.Status == PortStatus.Open)
                    .Select(p => $"<span class='port'>{p.Port}{(!string.IsNullOrEmpty(p.DisplayService) ? $"/{p.DisplayService}" : "")}</span>"))
                : "<span style='color:#4A5568'>—</span>";

            rows.AppendLine($@"<tr class='device-row' data-search=""{H(d.IpAddress)} {H(d.Hostname)} {H(d.Alias)} {H(d.MacAddress)} {H(d.Manufacturer)}"">
  <td><span class='dot' style='background:{statusColor};box-shadow:0 0 6px {statusColor}88'></span></td>
  <td><code>{H(d.IpAddress)}</code>{(d.IsNew ? " <span class='badge new'>NEU</span>" : "")}</td>
  <td>{H(d.Hostname)}</td>
  <td><em>{H(d.Alias)}</em></td>
  <td><code>{H(d.MacAddress)}</code></td>
  <td>{H(d.Manufacturer)}</td>
  <td>{(d.ResponseTime >= 0 ? $"{d.ResponseTime} ms" : "—")}</td>
  <td class='ports'>{portsHtml}</td>
  <td>{d.Category}</td>
  <td>{d.LastSeen:dd.MM.yyyy HH:mm}</td>
</tr>");
        }

        return $@"<!DOCTYPE html>
<html lang=""de"" data-theme=""{initialTheme}"">
<head>
<meta charset=""UTF-8""/>
<meta name=""viewport"" content=""width=device-width, initial-scale=1""/>
<title>Sideforge / NetworkLens Report — {H(scan.Subnet)} — {scan.TimestampFormatted}</title>
<style>
:root[data-theme=""dark""] {{
  --bg: #0F0F0F; --bg2: #1A1A1A; --card: #1F1F1F;
  --accent: #F07E2D; --text: #F5F5F5; --text2: #B4B2A9; --muted: #888780;
  --online: #74A732; --warn: #BA7517; --offline: #DC2626; --border: #2C2C2A;
  --hover: #2A1906;
}}
:root[data-theme=""light""] {{
  --bg: #FAFAF7; --bg2: #FFFFFF; --card: #F1EFE8;
  --accent: #E8600A; --text: #1A1A1A; --text2: #5F5E5A; --muted: #888780;
  --online: #639922; --warn: #BA7517; --offline: #991B1B; --border: #D3D1C7;
  --hover: #FDD9BC;
}}
*{{box-sizing:border-box;margin:0;padding:0}}
body{{background:var(--bg);color:var(--text);font-family:Verdana,sans-serif;font-size:14px;line-height:1.5;padding:24px}}
h1{{font-size:28px;font-weight:300;color:var(--text);margin-bottom:4px}}
h1 span{{color:var(--accent)}}
.subtitle{{color:var(--text2);font-size:13px;margin-bottom:28px}}
.stats{{display:flex;gap:16px;margin-bottom:28px;flex-wrap:wrap}}
.stat{{background:var(--card);border:1px solid var(--border);border-radius:8px;padding:16px 20px;min-width:140px}}
.stat-val{{font-size:28px;font-weight:300;color:var(--accent)}}
.stat-lbl{{font-size:11px;font-weight:600;color:var(--muted);text-transform:uppercase;letter-spacing:.5px;margin-top:4px}}
.toolbar{{display:flex;gap:10px;margin-bottom:16px;align-items:center}}
input[type=text]{{background:var(--bg2);border:1px solid var(--border);border-radius:5px;color:var(--text);padding:8px 12px 8px 32px;font-size:13px;width:280px;outline:none}}
input[type=text]:focus{{border-color:var(--accent)}}
.search-wrap{{position:relative;display:inline-block}}
.search-icon{{position:absolute;left:10px;top:50%;transform:translateY(-50%);color:var(--text2);font-size:14px;pointer-events:none}}
.search-clear{{position:absolute;right:6px;top:50%;transform:translateY(-50%);background:transparent;border:none;color:var(--text2);font-size:18px;cursor:pointer;width:22px;height:22px;border-radius:4px;padding:0;line-height:1;display:none}}
.search-wrap.has-text .search-clear{{display:block}}
.search-clear:hover{{background:var(--hover);color:var(--accent)}}
.theme-toggle{{background:var(--bg2);border:1px solid var(--border);border-radius:5px;color:var(--text);padding:7px 12px;font-size:12px;font-family:Verdana,sans-serif;cursor:pointer}}
.theme-toggle:hover{{border-color:var(--accent);color:var(--accent)}}

/* Ember-orange scrollbar */
::-webkit-scrollbar{{width:12px;height:12px}}
::-webkit-scrollbar-track{{background:var(--bg2)}}
::-webkit-scrollbar-thumb{{background:var(--accent);border-radius:6px;border:2px solid var(--bg2)}}
::-webkit-scrollbar-thumb:hover{{background:var(--accent)}}
html{{scrollbar-color:var(--accent) var(--bg2);scrollbar-width:thin}}
select{{background:var(--bg2);border:1px solid var(--border);border-radius:5px;color:var(--text);padding:8px 12px;font-size:13px;outline:none}}
table{{width:100%;border-collapse:collapse;background:var(--card);border-radius:8px;overflow:hidden;border:1px solid var(--border)}}
th{{background:var(--bg);color:var(--text2);font-size:11px;font-weight:600;text-transform:uppercase;letter-spacing:.5px;padding:11px 14px;text-align:left;border-bottom:1px solid var(--border);cursor:pointer;user-select:none}}
th:hover{{color:var(--text)}}
td{{padding:10px 14px;border-bottom:1px solid #1a1e24;vertical-align:middle}}
tr.device-row:hover td{{background:var(--hover)}}
.dot{{display:inline-block;width:8px;height:8px;border-radius:50%}}
code{{font-family:'Cascadia Code',Consolas,monospace;font-size:12px;color:var(--accent)}}
.port{{background:#1a0060;border:1px solid #330088;color:#b0aaff;border-radius:4px;padding:1px 5px;font-family:monospace;font-size:11px;margin:1px}}
.badge{{border-radius:4px;padding:2px 6px;font-size:10px;font-weight:700}}
.badge.new{{background:#1a00e644;border:1px solid #3300e688;color:var(--online)}}
.hidden{{display:none}}
footer{{margin-top:32px;text-align:center;color:var(--muted);font-size:12px}}
</style>
</head>
<body>
<div style=""display:flex;align-items:center;gap:12px;margin-bottom:8px"">
  <div style=""width:40px;height:40px;border-radius:8px;background:#1A1A1A;position:relative;display:inline-block"">
    <span style=""position:absolute;left:6px;top:50%;transform:translateY(-50%);font-family:Georgia;font-style:italic;font-weight:bold;font-size:24px;color:#E8600A"">S</span>
    <span style=""position:absolute;right:6px;top:50%;transform:translateY(-50%);font-family:Georgia;font-style:italic;font-weight:bold;font-size:24px;color:#F5F5F5"">F</span>
  </div>
  <h1 style=""margin:0""><strong style=""font-weight:bold"">Side</strong><span>forge</span> <span style=""color:var(--muted);font-weight:300;font-size:18px"">/ NetworkLens Report</span></h1>
</div>
<div class=""subtitle"">Subnetz: {H(scan.Subnet)} · {scan.TimestampFormatted} · Scan-Dauer: {scan.DurationFormatted}</div>

<div class=""stats"">
  <div class=""stat""><div class=""stat-val"">{scan.Devices.Count}</div><div class=""stat-lbl"">Geräte gesamt</div></div>
  <div class=""stat""><div class=""stat-val"" style=""color:var(--online)"">{online}</div><div class=""stat-lbl"">Online</div></div>
  <div class=""stat""><div class=""stat-val"" style=""color:var(--offline)"">{offline}</div><div class=""stat-lbl"">Offline</div></div>
  <div class=""stat""><div class=""stat-val"" style=""color:var(--warn)"">{newDev}</div><div class=""stat-lbl"">Neu erkannt</div></div>
  <div class=""stat""><div class=""stat-val"">{scan.TotalHostsScanned}</div><div class=""stat-lbl"">Hosts geprüft</div></div>
</div>

<div class=""toolbar"">
  <div class=""search-wrap"">
    <span class=""search-icon"">⌕</span>
    <input type=""text"" id=""search"" placeholder=""Suchen (IP, Name, MAC ...)"" oninput=""filterTable(this.value)""/>
    <button class=""search-clear"" onclick=""clearSearch()"" title=""Suche leeren"">×</button>
  </div>
  <select onchange=""filterStatus(this.value)"">
    <option value="""">Alle Status</option>
    <option value=""Online"">Online</option>
    <option value=""Offline"">Offline</option>
    <option value=""Slow"">Langsam</option>
  </select>
  <span style=""margin-left:auto;color:var(--text2);font-size:12px"" id=""rowcount"">{scan.Devices.Count} Einträge</span>
  <button class=""theme-toggle"" onclick=""toggleTheme()"" title=""Theme wechseln"" id=""themeBtn"">◐ Theme</button>
</div>

<table id=""devtable"">
<thead><tr>
  <th>●</th><th onclick=""sortTable(1)"">IP ↕</th><th onclick=""sortTable(2)"">Hostname ↕</th>
  <th>Alias</th><th>MAC</th><th>Hersteller</th><th onclick=""sortTable(6)"">Ping ↕</th>
  <th>Ports</th><th>Kategorie</th><th onclick=""sortTable(9)"">Zuletzt gesehen ↕</th>
</tr></thead>
<tbody id=""tbody"">{rows}</tbody>
</table>

<footer>Erstellt mit NetworkLens · {scan.TimestampFormatted}</footer>

<script>
let currentSearch='', currentStatus='';
function filterTable(q){{
  currentSearch=q.toLowerCase();
  document.querySelector('.search-wrap').classList.toggle('has-text', q.length > 0);
  applyFilters();
}}
function clearSearch(){{
  const inp=document.getElementById('search');
  inp.value='';
  currentSearch='';
  document.querySelector('.search-wrap').classList.remove('has-text');
  applyFilters();
  inp.focus();
}}
function toggleTheme(){{
  const html=document.documentElement;
  const cur=html.getAttribute('data-theme');
  html.setAttribute('data-theme', cur==='dark' ? 'light' : 'dark');
}}
function filterStatus(s){{currentStatus=s;applyFilters()}}
function applyFilters(){{
  const rows=document.querySelectorAll('#tbody .device-row');
  let vis=0;
  rows.forEach(r=>{{
    const txt=r.dataset.search.toLowerCase();
    const dot=r.querySelector('.dot');
    const statusOk=!currentStatus||dot?.style.background===getColor(currentStatus);
    const searchOk=!currentSearch||txt.includes(currentSearch);
    const show=statusOk&&searchOk;
    r.classList.toggle('hidden',!show);
    if(show)vis++;
  }});
  document.getElementById('rowcount').textContent=vis+' Einträge';
}}
function getColor(s){{return{{Online:'rgb(116, 167, 50)',Slow:'rgb(186, 117, 23)',Offline:'rgb(220, 38, 38)'}}[s]||''}}
function sortTable(col){{
  const tbody=document.getElementById('tbody');
  const rows=[...tbody.querySelectorAll('.device-row')];
  const asc=tbody.dataset.sortCol==col&&tbody.dataset.sortDir=='asc';
  rows.sort((a,b)=>{{
    const av=a.cells[col]?.textContent.trim()||'';
    const bv=b.cells[col]?.textContent.trim()||'';
    const n=v=>parseFloat(v)||0;
    return(col===1||col===6)?(asc?n(bv)-n(av):n(av)-n(bv)):asc?bv.localeCompare(av):av.localeCompare(bv);
  }});
  tbody.dataset.sortCol=col; tbody.dataset.sortDir=asc?'desc':'asc';
  rows.forEach(r=>tbody.appendChild(r));
}}
</script>
</body></html>";
    }

    private static string H(string? s) =>
        System.Net.WebUtility.HtmlEncode(s ?? "");
}
