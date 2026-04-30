using NetworkLens.Localization;
using NetworkLens.Models;

namespace NetworkLens.Services;

/// <summary>
/// Delivers notifications via Windows system sounds and debug output.
/// No external dependencies required.
/// </summary>
public sealed class ToastService : IDisposable
{
    private AppSettings _settings;

    public ToastService(AppSettings settings)
    {
        _settings = settings;
    }

    public void UpdateSettings(AppSettings s) => _settings = s;

    private void Show(string title, string message)
    {
        if (!_settings.NotificationsEnabled) return;
        if (_settings.Alerts.PlaySound)
            System.Media.SystemSounds.Exclamation.Play();
        System.Diagnostics.Debug.WriteLine($"[NetworkLens] {title}: {message}");
    }

    public void AlertNewDevice(NetworkDevice device)
    {
        if (!_settings.Alerts.NewDeviceAlert) return;
        Show(LocalizationManager.Instance.T("Toast_NewDeviceTitle"),
            $"{device.IpAddress}  {device.Hostname ?? ""}  {device.MacAddress ?? ""}");
    }

    public void AlertDeviceOffline(NetworkDevice device)
    {
        if (!_settings.Alerts.DeviceOfflineAlert) return;
        Show(LocalizationManager.Instance.T("Toast_OfflineTitle"), $"{device.DisplayName} ({device.IpAddress})");
    }

    public void AlertDeviceOnline(NetworkDevice device)
    {
        if (!_settings.Alerts.DeviceOnlineAlert) return;
        Show(LocalizationManager.Instance.T("Toast_OnlineTitle"), $"{device.DisplayName} ({device.IpAddress})");
    }

    public void AlertNewPort(NetworkDevice device, PortResult port)
    {
        if (!_settings.Alerts.NewOpenPortAlert) return;
        Show(LocalizationManager.Instance.T("Toast_NewPortTitle"),
            $"{device.DisplayName}: Port {port.Port} ({port.DisplayService})");
    }

    public void Dispose() { }
}

