using System.Collections.ObjectModel;
using System.Windows.Media;

namespace WarehouseManagementApp.Helpers;

public static class ColorPaletteHelper
{
    public static readonly string[] DefaultPresets =
    {
        "#EF4444", "#F97316", "#F59E0B", "#10B981",
        "#06B6D4", "#3B82F6", "#8B5CF6", "#EC4899"
    };

    public static ObservableCollection<string> CustomColors { get; } = new();

    public static void AddCustomColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;

        hex = hex.Trim().ToUpperInvariant();

        if (Array.Exists(DefaultPresets, p => p.Equals(hex, StringComparison.OrdinalIgnoreCase)))
            return;

        int existingIndex = -1;
        for (int i = 0; i < CustomColors.Count; i++)
        {
            if (CustomColors[i].Equals(hex, StringComparison.OrdinalIgnoreCase))
            {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex >= 0)
        {
            CustomColors.RemoveAt(existingIndex);
        }

        CustomColors.Insert(0, hex);

        while (CustomColors.Count > 20)
        {
            CustomColors.RemoveAt(CustomColors.Count - 1);
        }
    }

    public static Color HsvToRgb(double h, double s, double v)
    {
        int hi = (int)Math.Floor(h / 60.0) % 6;
        if (hi < 0) hi += 6;
        double f = (h / 60.0) - Math.Floor(h / 60.0);
        v *= 255;
        byte vVal = (byte)Math.Clamp(v, 0, 255);
        byte p = (byte)Math.Clamp(v * (1 - s), 0, 255);
        byte q = (byte)Math.Clamp(v * (1 - f * s), 0, 255);
        byte t = (byte)Math.Clamp(v * (1 - (1 - f) * s), 0, 255);

        return hi switch
        {
            0 => Color.FromRgb(vVal, t, p),
            1 => Color.FromRgb(q, vVal, p),
            2 => Color.FromRgb(p, vVal, t),
            3 => Color.FromRgb(p, q, vVal),
            4 => Color.FromRgb(t, p, vVal),
            _ => Color.FromRgb(vVal, p, q)
        };
    }

    public static void RgbToHsv(Color color, out double h, out double s, out double v)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        v = max;
        s = max == 0 ? 0 : delta / max;

        if (delta == 0) h = 0;
        else if (max == r) h = 60 * (((g - b) / delta) % 6);
        else if (max == g) h = 60 * (((b - r) / delta) + 2);
        else h = 60 * (((r - g) / delta) + 4);

        if (h < 0) h += 360;
    }

    public static string ColorToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static bool TryParseHex(string? hex, out Color color)
    {
        color = Colors.Transparent;
        if (string.IsNullOrWhiteSpace(hex)) return false;

        try
        {
            if (ColorConverter.ConvertFromString(hex.Trim()) is Color parsed)
            {
                color = parsed;
                return true;
            }
        }
        catch
        {
        }
        return false;
    }
}

