using System.Net;
using System.Net.Sockets;
using System.Text;
using NetworkLens.Models;

namespace NetworkLens.Services;

public class PortScanner
{
    private const int BannerTimeoutMs = 1000;
    private const int MaxBannerLength = 100;

    private int _timeoutMs = 500;
    private int _maxParallel = 200;
    private bool _grabBanners = true;

    public void Configure(int timeoutMs, int maxParallel, bool grabBanners = true)
    {
        _timeoutMs   = Math.Clamp(timeoutMs, 50, 10_000);
        _maxParallel = Math.Clamp(maxParallel, 1, 500);
        _grabBanners = grabBanners;
    }

    /// <summary>
    /// Scans the given ports. The host name is resolved once up front
    /// (previously every single port connect triggered its own DNS lookup).
    /// <paramref name="onResult"/> is invoked on thread-pool threads.
    /// </summary>
    public async Task ScanAsync(
        string host,
        IReadOnlyCollection<int> ports,
        Action<PortResult> onResult,
        CancellationToken cancellationToken = default)
    {
        var address = await ResolveAsync(host, cancellationToken).ConfigureAwait(false);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxParallel,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(ports, options, async (port, ct) =>
        {
            var result = await ScanPortAsync(address, host, port, ct).ConfigureAwait(false);
            onResult(result);
        }).ConfigureAwait(false);
    }

    public static async Task<IPAddress> ResolveAsync(string host, CancellationToken ct)
    {
        if (IPAddress.TryParse(host.Trim(), out var ip)) return ip;

        var addresses = await Dns.GetHostAddressesAsync(host.Trim(), ct).ConfigureAwait(false);
        return addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
               ?? addresses.FirstOrDefault()
               ?? throw new SocketException((int)SocketError.HostNotFound);
    }

    private async Task<PortResult> ScanPortAsync(IPAddress address, string host, int port, CancellationToken ct)
    {
        var result = new PortResult
        {
            Port = port,
            ServiceName = PortResult.GetServiceName(port)
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var tcp = new TcpClient(address.AddressFamily) { NoDelay = true };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_timeoutMs);

            await tcp.ConnectAsync(address, port, cts.Token).ConfigureAwait(false);
            sw.Stop();

            result.Status       = PortStatus.Open;
            result.ResponseTime = sw.ElapsedMilliseconds;

            if (_grabBanners && tcp.Connected)
                result.Banner = await TryGrabBannerAsync(tcp, host, port, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // user cancelled — propagate instead of reporting "filtered"
        }
        catch (OperationCanceledException)
        {
            result.Status = PortStatus.Filtered; // connect timeout
        }
        catch (SocketException ex)
        {
            result.Status = ex.SocketErrorCode == SocketError.ConnectionRefused
                ? PortStatus.Closed
                : PortStatus.Filtered;
        }
        catch
        {
            result.Status = PortStatus.Filtered;
        }

        return result;
    }

    private static async Task<string?> TryGrabBannerAsync(TcpClient tcp, string host, int port, CancellationToken ct)
    {
        try
        {
            var stream = tcp.GetStream();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(BannerTimeoutMs);

            // HTTP needs a request before it answers; most other services (SSH, FTP,
            // SMTP, POP3, IMAP) send their banner on connect.
            if (port is 80 or 8080 or 8000 or 8008 or 8081 or 8088 or 8888)
            {
                var safeHost = IPAddress.TryParse(host, out _) || Uri.CheckHostName(host) != UriHostNameType.Unknown
                    ? host
                    : "localhost";
                var probe = Encoding.ASCII.GetBytes($"HEAD / HTTP/1.0\r\nHost: {safeHost}\r\n\r\n");
                await stream.WriteAsync(probe, cts.Token).ConfigureAwait(false);
            }

            var buffer = new byte[512];
            int read = await stream.ReadAsync(buffer, cts.Token).ConfigureAwait(false);
            if (read <= 0) return null;

            var firstLine = Encoding.ASCII.GetString(buffer, 0, read).Split('\n')[0];
            var clean = new string(firstLine.Select(c => char.IsControl(c) ? ' ' : c).ToArray()).Trim();
            if (clean.Length == 0) return null;
            return clean.Length > MaxBannerLength ? clean[..MaxBannerLength] + "…" : clean;
        }
        catch
        {
            return null;
        }
    }
}
