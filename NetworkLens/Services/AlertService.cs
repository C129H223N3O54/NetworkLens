using NetworkLens.Models;

namespace NetworkLens.Services;

/// <summary>
/// Decides whether an event should notify the user, based on the
/// <b>current</b> settings (changes in Settings apply without restart).
/// </summary>
public class AlertService
{
    private readonly ToastService _toast = new();

    private static AppSettings Settings => SettingsStore.Current;

    public void NotifyNewDevice(NetworkDevice device)
    {
        if (!Settings.NotificationsEnabled || !Settings.Alerts.NewDeviceAlert) return;
        _toast.AlertNewDevice(device);
    }

    public void NotifyDeviceOffline(NetworkDevice device)
    {
        if (!Settings.NotificationsEnabled || !Settings.Alerts.DeviceOfflineAlert) return;
        _toast.AlertDeviceOffline(device);
    }

    public void NotifyDeviceOnline(NetworkDevice device)
    {
        if (!Settings.NotificationsEnabled || !Settings.Alerts.DeviceOnlineAlert) return;
        _toast.AlertDeviceOnline(device);
    }
}
