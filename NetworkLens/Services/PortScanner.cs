using System.Net.Sockets;
using System.Text;
using NetworkLens.Models;

namespace NetworkLens.Services;

public class PortScanner
{
    private int _timeoutMs = 500;
    private int _maxParallel = 200;
    private bool _grabBanners = true;

    public void Configure(int timeoutMs, int maxParallel, bool grabBanners = true)
    {
        _timeoutMs    = timeoutMs;
        _maxParallel  = maxParallel;
        _grabBanners  = grabBanners;
    }

    public async Task ScanAsync(
        string host,
        int[] ports,
        Action<PortResult> onResult,
        CancellationToken cancellationToken = default)
    {
        using var sem = new SemaphoreSlim(_maxParallel);

        var tasks = ports.Select(async port =>
        {
            await sem.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await ScanPortAsync(host, port, cancellationToken);
                onResult(result);
            }
            finally { sem.Release(); }
        });

        await Task.WhenAll(tasks);
    }

    private async Task<PortResult> ScanPortAsync(string host, int port, CancellationToken ct)
    {
        var result = new PortResult
        {
            Port = port,
            ServiceName = PortResult.GetServiceName(port)
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var tcp = new TcpClient();
            tcp.NoDelay = true;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_timeoutMs);

            await tcp.ConnectAsync(host, port, cts.Token);
            sw.Stop();

            result.Status       = PortStatus.Open;
            result.ResponseTime = sw.ElapsedMilliseconds;

            // Banner grabbing for open ports
            if (_grabBanners && tcp.Connected)
            {
                result.Banner = await TryGrabBannerAsync(tcp, port, ct);
            }
        }
        catch (OperationCanceledException)
        {
            result.Status = PortStatus.Filtered;
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

    private static async Task<string?> TryGrabBannerAsync(TcpClient tcp, int port, CancellationToken ct)
    {
        try
        {
            tcp.ReceiveTimeout = 1000;
            using var stream = tcp.GetStream();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(1000);

            // Send probe depending on service
            string? probe = port switch
            {
                80 or 8080 or 8000 or 8008 => "HEAD / HTTP/1.0\r\nHost: target\r\n\r\n",
                21 or 25 or 110 or 143     => null, // passive — just read
                22                          => null, // SSH sends banner first
                _                           => null
            };

            if (probe != null)
            {
                var bytes = Encoding.ASCII.GetBytes(probe);
                await stream.WriteAsync(bytes, cts.Token);
            }

            var buffer = new byte[512];
            int read = await stream.ReadAsync(buffer, cts.Token);
            if (read > 0)
            {
                var banner = Encoding.ASCII.GetString(buffer, 0, read).Trim();
                // Only first line
                var firstLine = banner.Split('\n')[0].Trim();
                return firstLine.Length > 100 ? firstLine[..100] + "…" : firstLine;
            }
        }
        catch { }
        return null;
    }
}
