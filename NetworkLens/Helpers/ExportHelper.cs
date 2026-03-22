using System.Diagnostics;
using Microsoft.Win32;

namespace NetworkLens.Helpers;

public static class ExportHelper
{
    /// <summary>Opens a Save File dialog and returns the chosen path, or null on cancel.</summary>
    public static string? ShowSaveDialog(string title, string defaultName, string filter)
    {
        var dlg = new SaveFileDialog
        {
            Title = title,
            FileName = defaultName,
            Filter = filter,
            AddExtension = true
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    /// <summary>Opens a file with the default system application.</summary>
    public static void OpenFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch { }
    }

    /// <summary>Opens a folder in Explorer.</summary>
    public static void OpenFolder(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
        }
        catch { }
    }

    /// <summary>Opens a URL in the default browser.</summary>
    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }

    /// <summary>Ensures a directory exists.</summary>
    public static void EnsureDir(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}
