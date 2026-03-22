using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NetworkLens.Models;

namespace NetworkLens.Services;

public class NetworkScanner
{
    private int _timeoutMs = 1000;
    private int _maxParallel = 100;

    public void Configure(int timeoutMs, int maxParallel)
    {
        _timeoutMs = timeoutMs;
        _maxParallel = maxParallel;
    }

    // ── Main scan entry point ──────────────────────
    public async Task ScanAsync(
        string subnetCidr,
        Action<NetworkDevice> onHostFound,
        Action<int, int> onProgress,
        CancellationToken cancellationToken = default)
    {
        var ips = IpHelper.GetAllIpsInSubnet(subnetCidr);
        int total = ips.Count;
        int done = 0;

        // Refresh ARP cache by pinging broadcast first (best effort)
        _ = Task.Run(() => TryPingBroadcast(subnetCidr), cancellationToken);

        using var sem = new SemaphoreSlim(_maxParallel);

        var tasks = ips.Select(async ip =>
        {
            await sem.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var device = await ProbeHostAsync(ip, cancellationToken);
                if (device != null)
                    onHostFound(device);

                var current = Interlocked.Increment(ref done);
                onProgress(current, total);
            }
            finally
            {
                sem.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    // ── Single host probe ──────────────────────────
    private async Task<NetworkDevice?> ProbeHostAsync(string ip, CancellationToken ct)
    {
        long ms = await PingAsync(ip, ct);
        if (ms < 0) return null;

        var device = new NetworkDevice
        {
            IpAddress    = ip,
            ResponseTime = ms,
            Status       = ms > 200 ? DeviceStatus.Slow : DeviceStatus.Online,
            LastSeen     = DateTime.Now
        };

        // DNS reverse lookup (fire and forget timeout)
        try
        {
            using var dnsCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            dnsCts.CancelAfter(1500);
            var entry = await Dns.GetHostEntryAsync(ip, dnsCts.Token);
            device.Hostname = entry.HostName;
        }
        catch { device.Hostname = ip; }

        // MAC from ARP cache
        device.MacAddress = GetMacFromArp(ip);

        return device;
    }

    // ── Ping ──────────────────────────────────────
    private async Task<long> PingAsync(string ip, CancellationToken ct)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ip, _timeoutMs);
            return reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
        }
        catch { return -1; }
    }

    // ── ARP Cache Lookup (Windows) ─────────────────
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(int destIp, int srcIp, byte[] pMacAddr, ref int phyAddrLen);

    public static string? GetMacFromArp(string ip)
    {
        try
        {
            if (!IPAddress.TryParse(ip, out var addr)) return null;

            // Try SendARP first (requires admin for remote IPs but still works on local subnet)
            byte[] mac = new byte[6];
            int len = 6;
            int dest = BitConverter.ToInt32(addr.GetAddressBytes(), 0);
            int ret = SendARP(dest, 0, mac, ref len);
            if (ret == 0 && mac.Any(b => b != 0))
            {
                return string.Join(":", mac.Take(len).Select(b => b.ToString("X2")));
            }
        }
        catch { }
        return null;
    }

    // ── Broadcast ping to populate ARP table ──────
    private static void TryPingBroadcast(string cidr)
    {
        try
        {
            var broadcast = IpHelper.GetBroadcastAddress(cidr);
            if (broadcast == null) return;
            using var ping = new Ping();
            ping.Send(broadcast, 200);
        }
        catch { }
    }
}
