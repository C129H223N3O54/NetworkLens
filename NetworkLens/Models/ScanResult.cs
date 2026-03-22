namespace NetworkLens.Models;

public class ScanResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string? Subnet { get; set; }
    public int TotalHostsScanned { get; set; }
    public TimeSpan Duration { get; set; }
    public List<NetworkDevice> Devices { get; set; } = new();

    // Computed
    public int OnlineCount => Devices.Count(d => d.Status == DeviceStatus.Online || d.Status == DeviceStatus.Slow);
    public int OfflineCount => Devices.Count(d => d.Status == DeviceStatus.Offline);
    public int NewDeviceCount { get; set; }

    public string DurationFormatted => Duration.TotalSeconds >= 60
        ? $"{Duration.Minutes}m {Duration.Seconds}s"
        : Duration.TotalSeconds >= 1
            ? $"{Duration.TotalSeconds:F1}s"
            : $"{Duration.TotalMilliseconds:F0}ms";

    public string TimestampFormatted => Timestamp.ToString("dd.MM.yyyy HH:mm:ss");
    public string FilenameSlug => Timestamp.ToString("yyyyMMdd_HHmmss");
}
