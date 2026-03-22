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
    public string ReportOutputPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "de";
    public AlertConfig Alerts { get; set; } = new();
    public string LastSubnet { get; set; } = "";
}
