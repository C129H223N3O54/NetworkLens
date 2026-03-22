using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    public Brush StatusBg { get; set; } = new SolidColorBrush(Color.FromArgb(0x20, 0x4A, 0x55, 0x68));
    public Brush StatusFg { get; set; } = new SolidColorBrush(Color.FromRgb(0x8A, 0x9B, 0xB0));
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
    private string? _publicIp;

    public NetworkInfoView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAllAsync();
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        LoadLocalNetworkInfo();
        LoadAdapters();
        LoadConnections();
        await LoadPublicIpAsync();
        await PingGatewayAsync();
    }

    // ── Local IP / Gateway / DNS ───────────────────
    private void LoadLocalNetworkInfo()
    {
        try
        {
            var dnsServers = new List<string>();

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback
                    or NetworkInterfaceType.Tunnel) continue;

                var props = nic.GetIPProperties();

                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    var ip = addr.Address.ToString();
                    if (ip.StartsWith("169.254.")) continue;

                    TxtLocalIp.Text = ip;
                    TxtSubnetMask.Text = $"Maske: {addr.IPv4Mask}";
                    TxtDhcp.Text = addr.PrefixOrigin == PrefixOrigin.Dhcp ? "DHCP" : "Statisch";

                    // Gateway
                    var gw = props.GatewayAddresses
                        .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork);
                    if (gw != null)
                        TxtGateway.Text = gw.Address.ToString();

                    break;
                }

                // Collect DNS
                foreach (var dns in props.DnsAddresses)
                {
                    if (dns.AddressFamily == AddressFamily.InterNetwork ||
                        dns.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        var s = dns.ToString();
                        if (!dnsServers.Contains(s)) dnsServers.Add(s);
                    }
                }

                // WLAN SSID detection
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    TryGetWlanInfo(nic);
                }
            }

            // DNS list
            if (dnsServers.Count > 0)
            {
                DnsList.ItemsSource = dnsServers;
                TxtNoDns.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtNoDns.Visibility = Visibility.Visible;
            }
        }
        catch { }
    }

    private void TryGetWlanInfo(NetworkInterface nic)
    {
        try
        {
            // Use netsh to get SSID
            var psi = new ProcessStartInfo("netsh", "wlan show interfaces")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(2000);

            foreach (var line in output.Split('\n'))
            {
                if (line.TrimStart().StartsWith("SSID") && !line.Contains("BSSID"))
                {
                    var ssid = line.Split(':').LastOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(ssid))
                    {
                        TxtSsid.Text = ssid;
                        TxtWlanAdapter.Text = nic.Name;
                        WlanPanel.Visibility = Visibility.Visible;
                        NoWlanPanel.Visibility = Visibility.Collapsed;
                    }
                    break;
                }
            }
        }
        catch { }
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
                : "Ping: Keine Antwort";
        }
        catch { TxtGatewayPing.Text = "Ping: Fehler"; }
    }

    // ── Public IP ─────────────────────────────────
    private async Task LoadPublicIpAsync()
    {
        try
        {
            using var http = new System.Net.Http.HttpClient();
            http.Timeout = TimeSpan.FromSeconds(5);
            _publicIp = (await http.GetStringAsync("https://api.ipify.org")).Trim();
            TxtPublicIp.Text = _publicIp;
        }
        catch
        {
            TxtPublicIp.Text = "Nicht verfügbar";
        }
    }

    private void BtnCopyPublicIp_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_publicIp))
            Clipboard.SetText(_publicIp);
    }

    // ── Adapter List ──────────────────────────────
    private void LoadAdapters()
    {
        var adapters = new List<AdapterInfo>();
        int active = 0;

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                     .OrderBy(n => n.OperationalStatus != OperationalStatus.Up)
                     .ThenBy(n => n.Name))
        {
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            var isUp = nic.OperationalStatus == OperationalStatus.Up;
            if (isUp) active++;

            var props = nic.GetIPProperties();
            var ipv4 = props.UnicastAddresses
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
                StatusText  = isUp ? "AKTIV" : "INAKTIV",
                Icon        = icon,
                StatusBg    = isUp
                    ? new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0xE6, 0x76))
                    : new SolidColorBrush(Color.FromArgb(0x18, 0x4A, 0x55, 0x68)),
                StatusFg    = isUp
                    ? new SolidColorBrush(Color.FromRgb(0x00, 0xE6, 0x76))
                    : new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68))
            });
        }

        AdapterList.ItemsSource = adapters;
        TxtAdapterCount.Text = $"{active} aktiv / {adapters.Count} gesamt";
    }

    // ── Active Connections (netstat) ──────────────
    private void BtnRefreshConnections_Click(object sender, RoutedEventArgs e)
    {
        LoadConnections();
    }

    private void LoadConnections()
    {
        try
        {
            var connections = GetActiveConnections();
            ConnectionGrid.ItemsSource = connections;
            TxtConnCount.Text = connections.Count.ToString();
        }
        catch { }
    }

    private static List<ConnectionInfo> GetActiveConnections()
    {
        var result = new List<ConnectionInfo>();

        try
        {
            // Parse netstat output
            var psi = new ProcessStartInfo("netstat", "-ano")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return result;

            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(3000);

            // Build PID → process name map
            var pidMap = new Dictionary<int, string>();
            try
            {
                foreach (var p in Process.GetProcesses())
                {
                    try { pidMap[p.Id] = p.ProcessName; }
                    catch { }
                }
            }
            catch { }

            foreach (var line in output.Split('\n'))
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;
                if (parts[0] is not "TCP" and not "UDP") continue;

                int pid = 0;
                string state = "";

                if (parts[0] == "TCP" && parts.Length >= 5)
                {
                    state = parts[3];
                    int.TryParse(parts[4], out pid);
                }
                else if (parts[0] == "UDP" && parts.Length >= 4)
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
            }
        }
        catch { }

        return result.Take(500).ToList(); // cap at 500 rows
    }
}
