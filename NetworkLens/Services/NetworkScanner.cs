using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using NetworkLens.Models;

namespace NetworkLens.Services;

public class NetworkScanner
{
    /// <summary>Largest scannable range: a /16 (65 534 hosts).</summary>
    public const int MaxHosts = 65_534;

    private const int DnsTimeoutMs = 1500;

    private int _timeoutMs = 1000;
    private int _maxParallel = 100;

    public void Configure(int timeoutMs, int maxParallel)
    {
        _timeoutMs   = Math.Clamp(timeoutMs, 100, 10_000);
        _maxParallel = Math.Clamp(maxParallel, 1, 500);
    }

    // ── Main scan entry point ──────────────────────
    /// <summary>
    /// Pings every host in the subnet with bounded parallelism.
    /// Callbacks are invoked on thread-pool threads.
    /// </summary>
    public async Task ScanAsync(
        string subnetCidr,
        Action<NetworkDevice> onHostFound,
        Action<int, int> onProgress,
        CancellationToken cancellationToken = default)
    {
        if (!IpHelper.TryGetHostRange(subnetCidr, out uint first, out uint last))
            throw new ArgumentException($"Invalid subnet: {subnetCidr}", nameof(subnetCidr));

        long count = IpHelper.HostCount(first, last);
        if (count > MaxHosts)
            throw new ArgumentOutOfRangeException(nameof(subnetCidr), count,
                $"Subnet too large ({count} hosts, max {MaxHosts}).");

        int total = (int)count;
        int done = 0;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxParallel,
            CancellationToken = cancellationToken
        };

        // Parallel.ForEachAsync streams the addresses lazily instead of creating
        // one Task per host up front (important for large subnets).
        await Parallel.ForEachAsync(IpHelper.EnumerateHosts(first, last), options, async (ip, ct) =>
        {
            var device = await ProbeHostAsync(ip, ct).ConfigureAwait(false);
            if (device != null)
                onHostFound(device);

            onProgress(Interlocked.Increment(ref done), total);
        }).ConfigureAwait(false);
    }

    // ── Single host probe ──────────────────────────
    private async Task<NetworkDevice?> ProbeHostAsync(IPAddress ip, CancellationToken ct)
    {
        long ms = await PingAsync(ip, ct).ConfigureAwait(false);
        if (ms < 0) return null;

        var ipText = ip.ToString();
        return new NetworkDevice
        {
            IpAddress    = ipText,
            ResponseTime = ms,
            Status       = ms > 200 ? DeviceStatus.Slow : DeviceStatus.Online,
            LastSeen     = DateTime.Now,
            Hostname     = await ResolveHostnameAsync(ip, ct).ConfigureAwait(false),
            MacAddress   = GetMacFromArp(ip)
        };
    }

    private async Task<long> PingAsync(IPAddress ip, CancellationToken ct)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ip, TimeSpan.FromMilliseconds(_timeoutMs),
                cancellationToken: ct).ConfigureAwait(false);
            return reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return -1;
        }
    }

    private static async Task<string> ResolveHostnameAsync(IPAddress ip, CancellationToken ct)
    {
        var ipText = ip.ToString();
        try
        {
            using var dnsCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            dnsCts.CancelAfter(DnsTimeoutMs);
            var entry = await Dns.GetHostEntryAsync(ipText, dnsCts.Token).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(entry.HostName) ? ipText : entry.HostName;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return ipText;
        }
    }

    // ── ARP Cache Lookup (Windows) ─────────────────
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(int destIp, int srcIp, byte[] pMacAddr, ref int phyAddrLen);

    public static string? GetMacFromArp(IPAddress addr)
    {
        try
        {
            byte[] mac = new byte[6];
            int len = mac.Length;
            int dest = BitConverter.ToInt32(addr.GetAddressBytes(), 0);
            if (SendARP(dest, 0, mac, ref len) == 0 && len > 0 && mac.Any(b => b != 0))
                return string.Join(":", mac.Take(len).Select(b => b.ToString("X2")));
        }
        catch
        {
            // ARP lookup is best effort (fails for hosts outside the local segment).
        }
        return null;
    }
}
