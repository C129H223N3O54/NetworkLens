namespace NetworkLens.Models;

public class AlertConfig
{
    public bool NewDeviceAlert { get; set; } = true;
    public bool DeviceOfflineAlert { get; set; } = true;
    public bool DeviceOnlineAlert { get; set; } = true;
    public bool NewOpenPortAlert { get; set; } = true;
    public bool PlaySound { get; set; } = false;
    public bool ShowToastNotification { get; set; } = true;
}

public class AppSettings
{
    public int ScanTimeoutMs { get; set; } = 1000;
    public int MaxParallelThreads { get; set; } = 100;
    public int PortScanTimeoutMs { get; set; } = 500;
    public int MonitorIntervalSeconds { get; set; } = 10;
    public bool AutoScanOnStart { get; set; } = false;
    public bool NotificationsEnabled { get; set; } = true;
    public string ReportOutputPath { get; set; } = DefaultReportPath;
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "de";
    public AlertConfig Alerts { get; set; } = new();
    public string LastSubnet { get; set; } = "";

    public static string DefaultReportPath =>
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    /// <summary>
    /// Clamps values loaded from disk into sane ranges so a hand-edited or
    /// corrupted config.json cannot produce 0 ms timeouts or 100 000 threads.
    /// </summary>
    public AppSettings Normalize()
    {
        ScanTimeoutMs          = Math.Clamp(ScanTimeoutMs, 100, 10_000);
        MaxParallelThreads     = Math.Clamp(MaxParallelThreads, 1, 500);
        PortScanTimeoutMs      = Math.Clamp(PortScanTimeoutMs, 50, 10_000);
        MonitorIntervalSeconds = Math.Clamp(MonitorIntervalSeconds, 0, 3600);
        Alerts ??= new AlertConfig();
        if (string.IsNullOrWhiteSpace(ReportOutputPath))
            ReportOutputPath = DefaultReportPath;
        return this;
    }
}
