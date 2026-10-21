namespace BloodborneLauncher.Core;

/// <summary>Choose only one bounded decode size; account for DPI and UniformToFill cropping.</summary>
internal static class BackdropResolutionPolicy
{
    public static int Width(double width,double height,double dpiScale=1)
    {
        if(!double.IsFinite(width)||!double.IsFinite(height)||!double.IsFinite(dpiScale)||width<=0||height<=0||dpiScale<=0)return 1280;
        double pixels=Math.Max(width,height*1920.0/1024)*dpiScale;
        return Math.Clamp((int)Math.Ceiling(pixels/320)*320,960,1920);
    }
}
