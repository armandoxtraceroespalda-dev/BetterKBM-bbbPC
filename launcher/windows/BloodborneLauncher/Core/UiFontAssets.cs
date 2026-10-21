using System.IO;
namespace BloodborneLauncher.Core;

/// <summary>Install the redistributable fallback separately from game fonts, in a versioned GUI directory.</summary>
internal static class UiFontAssets
{
    public const string Hash="a231a6018b35d73950461b3f40e99b13a8e0e5f47a9b53013b6dec433cc94e59";
    public static string Folder=>Path.Combine(AppContext.BaseDirectory,"gui","fonts",Hash[..12]);
    public static string FamilyTarget=>new Uri(Folder+Path.DirectorySeparatorChar).AbsoluteUri+"#Launcher UI Fallback";
    public static void EnsureInstalled(string? directory=null)
    {
        directory??=Folder;Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,"LauncherFallback.otf");
        if(File.Exists(path))return;
        using var source=typeof(UiFontAssets).Assembly.GetManifestResourceStream("BloodborneLauncher.Fonts.LauncherFallback.otf")
            ??throw new FileNotFoundException("The bundled UI fallback font is missing.");
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {using(var output=File.Create(temporary))source.CopyTo(output);File.Move(temporary,path,false);}
        catch(IOException) when(File.Exists(path)) { }
        finally {if(File.Exists(temporary))File.Delete(temporary);}
    }
}
