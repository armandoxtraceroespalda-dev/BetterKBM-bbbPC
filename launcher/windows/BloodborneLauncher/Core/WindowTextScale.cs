namespace BloodborneLauncher.Core;

/// <summary>Logical window dimensions already include Windows' DPI scaling.</summary>
public static class WindowTextScale
{
    public static double For(double width, double height)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0) return 1;
        double scale = Math.Sqrt((width / 1280) * (height / 800));
        return Math.Round(Math.Clamp(scale, 0.7, 1.3), 2);
    }
}
