using System.Drawing;
using System.Windows.Forms;
using NetworkLens.Models;

namespace NetworkLens.Services;

/// <summary>
/// Delivers Windows notifications via a hidden NotifyIcon (system tray balloon tip).
/// Works without app package identity — compatible with self-contained single-file exe.
/// </summary>
public sealed class ToastService : IDisposable
{
    private readonly NotifyIcon _tray;
    private AppSettings _settings;

    public ToastService(AppSettings settings)
    {
        _settings = settings;
        _tray = new NotifyIcon
        {
            Text    = "NetworkLens",
            Visible = false,
            Icon    = SystemIcons.Shield   // fallback; app icon loaded in Init()
        };
    }

    public void UpdateSettings(AppSettings s) => _settings = s;

    // ── Show tray balloon ─────────────────────────
    private void Show(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        if (!_settings.NotificationsEnabled) return;

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                _tray.Visible = true;
                _tray.ShowBalloonTip(4000, title, message, icon);

                if (_settings.Alerts.PlaySound)
                    System.Media.SystemSounds.Exclamation.Play();
            }
            catch { }
        });
    }

    // ── Public alert methods ──────────────────────

    public void AlertNewDevice(NetworkDevice device)
    {
        if (!_settings.Alerts.NewDeviceAlert) return;
        Show("⚠ Unbekanntes Gerät!",
            $"{device.IpAddress}  {device.Hostname ?? ""}  {device.MacAddress ?? ""}",
            ToolTipIcon.Warning);
    }

    public void AlertDeviceOffline(NetworkDevice device)
    {
        if (!_settings.Alerts.DeviceOfflineAlert) return;
        Show("🔴 Gerät offline",
            $"{device.DisplayName} ({device.IpAddress}) antwortet nicht.",
            ToolTipIcon.Warning);
    }

    public void AlertDeviceOnline(NetworkDevice device)
    {
        if (!_settings.Alerts.DeviceOnlineAlert) return;
        Show("🟢 Gerät wieder online",
            $"{device.DisplayName} ({device.IpAddress}) ist erreichbar.",
            ToolTipIcon.Info);
    }

    public void AlertNewPort(NetworkDevice device, PortResult port)
    {
        if (!_settings.Alerts.NewOpenPortAlert) return;
        Show("⚡ Neuer offener Port",
            $"{device.DisplayName}: Port {port.Port} ({port.DisplayService}) offen.",
            ToolTipIcon.Info);
    }

    // ── Hide tray icon when idle ──────────────────
    public void Hide() => _tray.Visible = false;

    public void Dispose()
    {
        _tray.Visible = false;
        _tray.Dispose();
    }
}
