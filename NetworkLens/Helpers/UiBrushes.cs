namespace NetworkLens.Helpers;

/// <summary>
/// Shared, frozen brushes for status colors.
/// Frozen brushes are immutable and thread-safe, so they can be created once
/// and reused by every row / binding instead of allocating a new brush per access.
/// </summary>
internal static class UiBrushes
{
    public static readonly Color OnlineColor  = Color.FromRgb(0x74, 0xA7, 0x32);
    public static readonly Color WarnColor    = Color.FromRgb(0xBA, 0x75, 0x17);
    public static readonly Color OfflineColor = Color.FromRgb(0xDC, 0x26, 0x26);
    public static readonly Color NeutralColor = Color.FromRgb(0x4A, 0x55, 0x68);
    public static readonly Color AccentColor  = Color.FromRgb(0xE8, 0x60, 0x0A);
    public static readonly Color MutedColor   = Color.FromRgb(0x88, 0x87, 0x80);
    public static readonly Color LightBlueColor = Color.FromRgb(0x00, 0x77, 0xA8);

    public static readonly Brush Online  = Solid(OnlineColor);
    public static readonly Brush Warn    = Solid(WarnColor);
    public static readonly Brush Offline = Solid(OfflineColor);
    public static readonly Brush Neutral = Solid(NeutralColor);
    public static readonly Brush Accent  = Solid(AccentColor);
    public static readonly Brush Muted   = Solid(MutedColor);
    public static readonly Brush LightBlue = Solid(LightBlueColor);

    // Translucent backgrounds (status pills)
    public static readonly Brush OnlineBg  = Solid(Color.FromArgb(0x22, 0x74, 0xA7, 0x32));
    public static readonly Brush WarnBg    = Solid(Color.FromArgb(0x22, 0xBA, 0x75, 0x17));
    public static readonly Brush NeutralBg = Solid(Color.FromArgb(0x18, 0x4A, 0x55, 0x68));
    public static readonly Brush ActiveBadgeBg   = Solid(Color.FromArgb(0x20, 0x74, 0xA7, 0x32));
    public static readonly Brush InactiveBadgeBg = Solid(Color.FromArgb(0x18, 0x4A, 0x55, 0x68));

    public static Brush Solid(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
