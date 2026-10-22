namespace BloodborneLauncher.Core;

/// <summary>Allow WPF hardware acceleration by default; software rendering remains an explicit diagnostic option.</summary>
internal static class LauncherRenderPolicy
{
    public static bool UseSoftware(IReadOnlyCollection<string> arguments) =>
        arguments.Contains("--software-rendering");
}
