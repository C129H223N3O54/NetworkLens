using System.Text.Json;
using NetworkLens.Models;

namespace NetworkLens.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private AppSettings _settings = new();
    public AppSettings Settings
    {
        get => _settings;
        set => Set(ref _settings, value);
    }

    private bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        set => Set(ref _isDirty, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand ResetCommand { get; }

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(Save, () => IsDirty);
        ResetCommand = new RelayCommand(Reset);
        Load();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(App.ConfigPath))
            {
                var json = File.ReadAllText(App.ConfigPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { Settings = new AppSettings(); }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(App.ConfigPath, json);
            IsDirty = false;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Einstellungen konnten nicht gespeichert werden:\n{ex.Message}",
                "Fehler", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private void Reset()
    {
        Settings = new AppSettings();
        IsDirty = true;
    }

    public static AppSettings LoadStatic()
    {
        try
        {
            if (File.Exists(App.ConfigPath))
            {
                var json = File.ReadAllText(App.ConfigPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }
}
