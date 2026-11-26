using NetworkLens.Localization;
using NetworkLens.Models;

namespace NetworkLens.Services;

/// <summary>
/// Delivers notifications via a Windows system sound (if enabled) and the log.
/// Note: real Windows toast notifications are not implemented yet (see ROADMAP.md).
/// </summary>
public sealed class ToastService
{
    private void Show(string title, string message)
    {
        if (SettingsStore.Current.Alerts.PlaySound)
            System.Media.SystemSounds.Exclamation.Play();
        Helpers.Log.Info($"Notification: {title}: {message}");
    }

    public void AlertNewDevice(NetworkDevice device) =>
        Show(LocalizationManager.Instance.T("Toast_NewDeviceTitle"),
            $"{device.IpAddress}  {device.Hostname ?? ""}  {device.MacAddress ?? ""}");

    public void AlertDeviceOffline(NetworkDevice device) =>
        Show(LocalizationManager.Instance.T("Toast_OfflineTitle"), $"{device.DisplayName} ({device.IpAddress})");

    public void AlertDeviceOnline(NetworkDevice device) =>
        Show(LocalizationManager.Instance.T("Toast_OnlineTitle"), $"{device.DisplayName} ({device.IpAddress})");
}
