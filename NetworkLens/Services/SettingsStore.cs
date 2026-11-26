using System.Text.Json;
using NetworkLens.Helpers;
using NetworkLens.Models;

namespace NetworkLens.Services;

/// <summary>
/// Single source of truth for <see cref="AppSettings"/>.
/// Services read <see cref="Current"/> at the moment they need a value, so changes
/// saved in the Settings view take effect immediately — without an app restart.
/// </summary>
public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private static readonly object Gate = new();
    private static AppSettings? _current;

    /// <summary>Raised after settings were saved.</summary>
    public static event EventHandler? Changed;

    public static AppSettings Current
    {
        get
        {
            lock (Gate)
                return _current ??= Load();
        }
    }

    /// <summary>Reads config.json from disk (fresh copy, safe to edit).</summary>
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(App.ConfigPath))
            {
                var json = File.ReadAllText(App.ConfigPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings.Normalize();
            }
        }
        catch (Exception ex)
        {
            Log.Warn("SettingsStore.Load", ex);
        }
        return new AppSettings();
    }

    /// <summary>Persists settings atomically and makes them the current settings.</summary>
    public static void Save(AppSettings settings)
    {
        settings.Normalize();
        var json = JsonSerializer.Serialize(settings, JsonOpts);
        AtomicFile.WriteAllText(App.ConfigPath, json);

        lock (Gate)
            _current = settings;

        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Report output folder; falls back to the desktop (or AppData) if the configured
    /// folder is empty or cannot be created.
    /// </summary>
    public static string ResolveReportPath()
    {
        foreach (var candidate in new[]
                 {
                     Current.ReportOutputPath,
                     AppSettings.DefaultReportPath,
                     App.AppDataPath
                 })
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            try
            {
                Directory.CreateDirectory(candidate);
                return candidate;
            }
            catch (Exception ex)
            {
                Log.Warn($"ResolveReportPath({candidate})", ex);
            }
        }
        return App.AppDataPath;
    }
}
