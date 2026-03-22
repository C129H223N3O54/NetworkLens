using NetworkLens.Models;

namespace NetworkLens.Services;

public class AlertService : IDisposable
{
    private AppSettings _settings;
    private readonly ToastService _toast;

    public AlertService(AppSettings settings)
    {
        _settings = settings;
        _toast = new ToastService(settings);
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        _toast.UpdateSettings(settings);
    }

    public void NotifyNewDevice(NetworkDevice device)
    {
        if (!_settings.NotificationsEnabled || !_settings.Alerts.NewDeviceAlert) return;
        _toast.AlertNewDevice(device);
    }

    public void NotifyDeviceOffline(NetworkDevice device)
    {
        if (!_settings.NotificationsEnabled || !_settings.Alerts.DeviceOfflineAlert) return;
        _toast.AlertDeviceOffline(device);
    }

    public void NotifyDeviceOnline(NetworkDevice device)
    {
        if (!_settings.NotificationsEnabled || !_settings.Alerts.DeviceOnlineAlert) return;
        _toast.AlertDeviceOnline(device);
    }

    public void NotifyNewOpenPort(NetworkDevice device, PortResult port)
    {
        if (!_settings.NotificationsEnabled || !_settings.Alerts.NewOpenPortAlert) return;
        _toast.AlertNewPort(device, port);
    }

    public void Dispose() => _toast.Dispose();
}
