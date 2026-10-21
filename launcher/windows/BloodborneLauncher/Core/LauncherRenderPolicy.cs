namespace BloodborneLauncher.Core;

/// <summary>Use the measured lower-memory software path for the launcher UI; never alter the game renderer.</summary>
internal static class LauncherRenderPolicy
{
    public static bool UseSoftware(IReadOnlyCollection<string> arguments) =>
        arguments.Contains("--software-rendering") || !arguments.Contains("--default-rendering");
}
