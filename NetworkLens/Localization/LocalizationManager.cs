using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace NetworkLens.Localization;

/// <summary>
/// Runtime localization with live switching.
/// Use {Binding [Key], Source={x:Static loc:LocalizationManager.Instance}} in XAML.
/// </summary>
public sealed class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new();

    private string _language = "de";
    public string Language
    {
        get => _language;
        set
        {
            if (_language == value) return;
            _language = value;
            // Notify all bindings to refresh by triggering indexer change
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    /// <summary>Indexer used for XAML binding: {Binding [Key]}</summary>
    public string this[string key] => Strings.Get(key, _language);

    /// <summary>Direct lookup from code.</summary>
    public string T(string key) => Strings.Get(key, _language);

    /// <summary>Wikipedia URL prefix for current language.</summary>
    public string WikiPrefix => _language == "en"
        ? "https://en.wikipedia.org/wiki/"
        : "https://de.wikipedia.org/wiki/";

    // ── Settings persistence ─────────────────────
    private static string LangFile =>
        Path.Combine(App.AppDataPath, "language.json");

    public static string DetectInitialLanguage()
    {
        try
        {
            if (File.Exists(LangFile))
            {
                var json = File.ReadAllText(LangFile);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("language", out var lang))
                    return lang.GetString() == "en" ? "en" : "de";
            }
        }
        catch { }

        // Auto-detect from Windows system language
        var sys = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return sys == "en" ? "en" : "de";
    }

    public void SaveLanguage()
    {
        try
        {
            Directory.CreateDirectory(App.AppDataPath);
            var json = JsonSerializer.Serialize(new { language = _language });
            File.WriteAllText(LangFile, json);
        }
        catch { }
    }
}
