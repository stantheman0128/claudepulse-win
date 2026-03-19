using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ClaudePulse.UI;

public static class IconGenerator
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private static readonly Dictionary<string, Icon> _cache = new();

    public static Icon Idle => GetOrCreate("idle", Color.FromArgb(76, 175, 80));
    public static Icon Working => GetOrCreate("working", Color.FromArgb(33, 150, 243));
    public static Icon Waiting => GetOrCreate("waiting", Color.FromArgb(255, 152, 0));
    public static Icon Stale => GetOrCreate("stale", Color.FromArgb(158, 158, 158));
    public static Icon Error => GetOrCreate("error", Color.FromArgb(244, 67, 54));

    private static Icon GetOrCreate(string key, Color color)
    {
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var icon = CreateCircleIcon(color);
        _cache[key] = icon;
        return icon;
    }

    private static Icon CreateCircleIcon(Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 13, 13);

        // Subtle border
        using var pen = new Pen(Color.FromArgb(60, 0, 0, 0), 0.5f);
        g.DrawEllipse(pen, 1, 1, 13, 13);

        var hIcon = bmp.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        // Clone to own the icon data, then destroy the GDI handle
        var cloned = (Icon)icon.Clone();
        DestroyIcon(hIcon);
        return cloned;
    }
}
