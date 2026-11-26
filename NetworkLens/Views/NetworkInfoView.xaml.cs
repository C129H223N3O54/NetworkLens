using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NetworkLens.Helpers;
using NetworkLens.Localization;
using NetworkLens.Services;

namespace NetworkLens.Views;

// ── Data models for this view ──────────────────────────
public class AdapterInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string SpeedText { get; set; } = "";
    public string AdapterType { get; set; } = "";
    public string StatusText { get; set; } = "";
    public string Icon { get; set; } = "🔌";
    public bool IsUp { get; set; }
    public Brush StatusBg { get; set; } = UiBrushes.InactiveBadgeBg;
    public Brush StatusFg { get; set; } = UiBrushes.Neutral;
}

public class ConnectionInfo
{
    public string Protocol { get; set; } = "";
    public string LocalAddress { get; set; } = "";
    public string RemoteAddress { get; set; } = "";
    public string State { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public int Pid { get; set; }
}

public partial class NetworkInfoView : UserControl
{
    private const int MaxConnections = 500;

    // One shared client for the app lifetime (avoids socket exhaustion).
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    private string? _publicIp;
    private bool _loadedOnce;
    private bool _isLoading;

    private static LocalizationManager L => LocalizationManager.Instance;

    private sealed record LocalInfo(
        string? Ip, string? Mask, bool IsDhcp, string? Gateway,
        List<string> DnsServers, string? Ssid, string? WlanAdapter);

    public NetworkInfoView()
    {
        InitializeComponent();

        // All views are created at app start but this one is hidden. Loading it eagerly ran
        // netstat/netsh/Process.GetProcesses on the UI thread and contacted api.ipify.org
        // on every launch. Now it loads the first time the tab becomes visible.
        IsVisibleChanged += async (_, e) =>
        {
            if (e.NewValue is true && !_loadedOnce)
                await LoadAllAsync();
        };

        LocalizationManager.Instance.LanguageChanged += async (_, _) =>
        {
            if (_loadedOnce) await LoadAllAsync();
        };
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Intentionally empty — see IsVisibleChanged in the constructor.
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        if (_isLoading) return;
        _isLoading = true;
        _loadedOnce = true;
        try
        {
            var localTask       = Task.Run(() => CollectLocalInfo());
            var adaptersTask    = Task.Run(() => CollectAdapters());
            var connectionsTask = Task.Run(() => GetActiveConnections());

            ApplyLocalInfo(await localTask);
            ApplyAdapters(await adaptersTask);
            ApplyConnections(await connectionsTask);

            await Task.WhenAll(LoadPublicIpAsync(), PingGatewayAsync());
        }
        catch (Exception ex)
        {
            Log.Warn("NetworkInfo.LoadAll", ex);
        }
        finally
        {
            _isLoading = false;
        }
    }

    // ── Local IP / Gateway / DNS (background) ──────
    private static LocalInfo CollectLocalInfo()
    {
        string? ip = null, mask = null, gateway = null;
        bool dhcp = false;

        var primary = IpHelper.GetPrimaryInterface();
        if (primary.HasValue)
        {
            var (nic, addr) = primary.Value;
            ip   = addr.Address.ToString();
            mask = addr.IPv4Mask?.ToString();
            dhcp = addr.PrefixOrigin == PrefixOrigin.Dhcp;
            gateway = nic.GetIPProperties().GatewayAddresses
                .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork)
                ?.Address.ToString();
        }

        var dns = new List<string>();
        bool hasWlan = false;
        try
        {
            foreach (var n in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (n.OperationalStatus != OperationalStatus.Up) continue;
                if (n.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

                foreach (var d in n.GetIPProperties().DnsAddresses)
                {
                    if (d.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
                    {
                        var s = d.ToString();
                        if (!dns.Contains(s)) dns.Add(s);
                    }
                }

                if (n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) hasWlan = true;
            }
        }
        catch (Exception ex)
        {
            Log.Warn("NetworkInfo.Dns", ex);
        }

        var (ssid, wlanAdapter) = hasWlan ? TryGetWlanInfo() : ((string?)null, (string?)null);
        return new LocalInfo(ip, mask, dhcp, gateway, dns, ssid, wlanAdapter);
    }

    private void ApplyLocalInfo(LocalInfo info)
    {
        if (info.Ip != null)
        {
            TxtLocalIp.Text    = info.Ip;
            TxtSubnetMask.Text = $"{L.T("Net_MaskLabel")}: {info.Mask}";
            TxtDhcp.Text       = info.IsDhcp ? "DHCP" : L.T("Net_Static");
        }
        TxtGateway.Text = info.Gateway ?? "—";

        if (info.DnsServers.Count > 0)
        {
            DnsList.ItemsSource = info.DnsServers;
            TxtNoDns.Visibility = Visibility.Collapsed;
        }
        else
        {
            TxtNoDns.Visibility = Visibility.Visible;
        }

        bool hasSsid = !string.IsNullOrEmpty(info.Ssid);
        if (hasSsid)
        {
            TxtSsid.Text        = info.Ssid;
            TxtWlanAdapter.Text = info.WlanAdapter ?? "";
        }
        WlanPanel.Visibility   = hasSsid ? Visibility.Visible : Visibility.Collapsed;
        NoWlanPanel.Visibility = hasSsid ? Visibility.Collapsed : Visibility.Visible;
    }

    private static (string? Ssid, string? Adapter) TryGetWlanInfo()
    {
        try
        {
            var output = RunTool("netsh.exe", "wlan show interfaces", 2000);
            string? adapter = null;

            foreach (var raw in output.Split('\n'))
            {
                var line = raw.Trim();
                int colon = line.IndexOf(':');
                if (colon < 0) continue;

                var label = line[..colon].Trim();
                var value = line[(colon + 1)..].Trim(); // SSIDs may contain ':'

                if (label is "Name") adapter = value;
                if (label == "SSID" && value.Length > 0) return (value, adapter);
            }
        }
        catch (Exception ex)
        {
            Log.Warn("NetworkInfo.Wlan", ex);
        }
        return (null, null);
    }

    // ── Gateway ping ──────────────────────────────
    private async Task PingGatewayAsync()
    {
        var gw = TxtGateway.Text;
        if (string.IsNullOrEmpty(gw) || gw == "—") return;
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(gw, 1000);
            TxtGatewayPing.Text = reply.Status == IPStatus.Success
                ? $"Ping: {reply.RoundtripTime} ms"
                : L.T("Net_NoReply");
        }
        catch
        {
            TxtGatewayPing.Text = L.T("Stat_PingError");
        }
    }

    // ── Public IP ─────────────────────────────────
    private async Task LoadPublicIpAsync()
    {
        try
        {
            var text = (await Http.GetStringAsync("https://api.ipify.org")).Trim();
            // Only accept something that actually is an IP address
            _publicIp = ShellHelper.NormalizeIp(text);
            TxtPublicIp.Text = _publicIp ?? L.T("Net_Unavailable");
        }
        catch
        {
            _publicIp = null;
            TxtPublicIp.Text = L.T("Net_Unavailable");
        }
    }

    private void BtnCopyPublicIp_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_publicIp))
            Clipboard.SetText(_publicIp);
    }

    // ── Adapter List (background) ─────────────────
    private static List<AdapterInfo> CollectAdapters()
    {
        var adapters = new List<AdapterInfo>();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                     .OrderBy(n => n.OperationalStatus != OperationalStatus.Up)
                     .ThenBy(n => n.Name))
        {
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            var isUp = nic.OperationalStatus == OperationalStatus.Up;

            var ipv4 = nic.GetIPProperties().UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                ?.Address.ToString() ?? "—";

            var mac = string.Join(":",
                nic.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
            if (mac == "") mac = "—";

            long speedMbps = isUp && nic.Speed > 0 ? nic.Speed / 1_000_000 : 0;
            string speedText = speedMbps >= 1000
                ? $"{speedMbps / 1000} Gbps"
                : speedMbps > 0 ? $"{speedMbps} Mbps" : "—";

            string icon = nic.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Wireless80211 => "📶",
                NetworkInterfaceType.Ethernet      => "🔌",
                NetworkInterfaceType.Tunnel        => "🔒",
                _                                  => "⬡"
            };

            adapters.Add(new AdapterInfo
            {
                Name        = nic.Name,
                Description = nic.Description,
                IpAddress   = ipv4,
                MacAddress  = mac,
                SpeedText   = speedText,
                AdapterType = nic.NetworkInterfaceType.ToString(),
                StatusText  = L.T(isUp ? "NetInfo_BadgeActive" : "NetInfo_BadgeInactive"),
                Icon        = icon,
                IsUp        = isUp,
                // Frozen brushes: safe to create on a background thread
                StatusBg    = isUp ? UiBrushes.ActiveBadgeBg : UiBrushes.InactiveBadgeBg,
                StatusFg    = isUp ? UiBrushes.Online : UiBrushes.Neutral
            });
        }

        return adapters;
    }

    private void ApplyAdapters(List<AdapterInfo> adapters)
    {
        AdapterList.ItemsSource = adapters;
        int active = adapters.Count(a => a.IsUp);
        TxtAdapterCount.Text = string.Format(L.T("Net_AdapterStatsFmt"), active, adapters.Count);
    }

    // ── Active Connections (netstat, background) ──
    private async void BtnRefreshConnections_Click(object sender, RoutedEventArgs e)
    {
        ApplyConnections(await Task.Run(() => GetActiveConnections()));
    }

    private void ApplyConnections(List<ConnectionInfo> connections)
    {
        ConnectionGrid.ItemsSource = connections;
        TxtConnCount.Text = connections.Count.ToString();
    }

    private static List<ConnectionInfo> GetActiveConnections()
    {
        var result = new List<ConnectionInfo>();

        try
        {
            var output = RunTool("netstat.exe", "-ano", 5000,
                System.Text.Encoding.GetEncoding(
                    System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage));

            // PID → process name map (Process objects hold OS handles → dispose them)
            var pidMap = new Dictionary<int, string>();
            foreach (var p in Process.GetProcesses())
            {
                using (p)
                {
                    try { pidMap[p.Id] = p.ProcessName; }
                    catch { /* process exited meanwhile */ }
                }
            }

            foreach (var line in output.Split('\n'))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;
                if (parts[0] is not "TCP" and not "UDP") continue;

                int pid = 0;
                string state = "";

                if (parts[0] == "TCP" && parts.Length >= 5)
                {
                    state = LocalizeState(parts[3]);
                    int.TryParse(parts[4], out pid);
                }
                else if (parts[0] == "UDP")
                {
                    int.TryParse(parts[3], out pid);
                }

                result.Add(new ConnectionInfo
                {
                    Protocol      = parts[0],
                    LocalAddress  = parts[1],
                    RemoteAddress = parts.Length > 2 ? parts[2] : "",
                    State         = state,
                    Pid           = pid,
                    ProcessName   = pidMap.TryGetValue(pid, out var pname) ? pname : ""
                });

                if (result.Count >= MaxConnections) break;
            }
        }
        catch (Exception ex)
        {
            Log.Warn("NetworkInfo.Netstat", ex);
        }

        return result;
    }

    /// <summary>Maps localized netstat states (German / English) to a translated label.</summary>
    private static string LocalizeState(string s)
    {
        var u = s.ToUpperInvariant();
        if (u.Contains("LISTEN") || u.Contains("ABH"))
            return L.T("Conn_Listen");
        if (u.Contains("ESTABLISHED") || u.Contains("HERGESTELLT") || u.Contains("VERBUNDEN"))
            return L.T("Conn_Established");
        if (u.Contains("TIME_WAIT") || u.Contains("WARTEZEIT") || u.Contains("WARTET"))
            return L.T("Conn_TimeWait");
        if (u.Contains("CLOSE_WAIT") || u.Contains("SCHLIESSWARTE") || u.Contains("SCHLIESST"))
            return L.T("Conn_CloseWait");
        return s;
    }

    /// <summary>
    /// Runs a System32 tool by absolute path (no PATH / app-directory search) and returns stdout.
    /// </summary>
    private static string RunTool(string exe, string args, int timeoutMs, System.Text.Encoding? encoding = null)
    {
        var psi = new ProcessStartInfo(ShellHelper.SystemExe(exe), args)
        {
            RedirectStandardOutput = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
            WorkingDirectory       = Environment.SystemDirectory
        };
        if (encoding != null) psi.StandardOutputEncoding = encoding;

        using var proc = Process.Start(psi);
        if (proc == null) return "";

        var outputTask = proc.StandardOutput.ReadToEndAsync();
        if (!proc.WaitForExit(timeoutMs))
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
        }
        return outputTask.Wait(timeoutMs) ? outputTask.Result : "";
    }
}
