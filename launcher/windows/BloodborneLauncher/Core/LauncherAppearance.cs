using System.IO;
using System.Windows;
using System.Windows.Media;
namespace BloodborneLauncher.Core;

/// <summary>One startup-selected layout; all layouts share the same pages and settings.</summary>
public static class LauncherAppearance
{
    public static string Layout => "bloodborne";
    public static bool Bloodborne => true;
    private static string BundledFallback=>UiFontAssets.FamilyTarget;
    public static FontFamily DetailFont { get; } = Composite("Segoe UI, Arial");
    private static FontFamily Composite(string latin)
    {
        var font=new FontFamily();
        font.FamilyMaps.Add(new FontFamilyMap {Unicode="0000-024F,1E00-1EFF",Target=latin+", Segoe UI"});
        font.FamilyMaps.Add(new FontFamilyMap {Unicode="0400-052F",Target="Georgia, Segoe UI, "+BundledFallback});
        font.FamilyMaps.Add(new FontFamilyMap {Unicode="0000-FFFF",Target=BundledFallback+", Segoe UI"});
        return font;
    }
    public static FontFamily MenuFont { get; private set; } = Composite("Palatino Linotype, Georgia, Times New Roman");
    public static bool HasLocalFont { get; private set; }
    public static string Normalize(string? value) => "bloodborne";
    public static void Select(string? value) { }

    public static void ApplyBloodborne()
    {
        // Native game font/art files are local assets, never downloaded or redistributed.
        MenuAssets.PrepareDirectory();
        var assets = MenuAssets.DirectoryPath;
        HasLocalFont = false;
        try
        {
            if (Directory.Exists(assets))
            {
                var fonts = Directory.EnumerateFiles(assets).Where(p =>
                    Path.GetExtension(p).Equals(".ttf", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(p).Equals(".otf", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (fonts.Length > 0)
                {
                    var path = fonts.OrderBy(p => p, StringComparer.Ordinal).First();
                    LauncherDiagnostics.Record($"Native font file: {path}; bytes={new FileInfo(path).Length}",LogLevel.Info);
                    using (LauncherDiagnostics.Measure("Local font resolution"))
                    {
                        var glyph = new GlyphTypeface(new Uri(path));
                        var family = glyph.FamilyNames.Values.First();
                        MenuFont = Composite(new Uri(assets+Path.DirectorySeparatorChar).AbsoluteUri+"#"+family);
                        HasLocalFont = true;
                    }
                }
            }
        }
        catch (Exception e) when (e is IOException or ArgumentException or NotSupportedException or System.Security.SecurityException or UnauthorizedAccessException or FormatException or InvalidOperationException)
        {
            LauncherDiagnostics.Record("Native font could not be loaded: "+e,LogLevel.Bad);
        }
        var resources = Application.Current.Resources;
        // App.xaml already installs this theme. Do not retain a second complete dictionary.
        var theme = resources.MergedDictionaries.FirstOrDefault(d =>
            d.Source?.OriginalString.EndsWith("Themes/Bloodborne.xaml",StringComparison.OrdinalIgnoreCase) == true);
        using (LauncherDiagnostics.Measure("Theme resource initialization"))
            if (theme != null)
                foreach (var key in theme.Keys.OfType<string>().Where(key=>key.EndsWith("Brush",StringComparison.Ordinal)))
                    if(theme[key] is Freezable drawing && drawing.CanFreeze)drawing.Freeze();
        LauncherDiagnostics.Record($"Native font loaded: {HasLocalFont}; {MenuFont.Source}",HasLocalFont?LogLevel.Good:LogLevel.Bad);
        resources["BloodborneFont"] = MenuFont;
        resources["DetailFont"] = DetailFont;
        resources["ContentControlThemeFontFamily"] = MenuFont;
        resources["ControlContentThemeFontFamily"] = MenuFont;
    }
}
