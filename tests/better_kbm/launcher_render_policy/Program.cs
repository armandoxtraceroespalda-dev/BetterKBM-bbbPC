using BloodborneLauncher.Core;
static void Check(bool value,string message){if(!value)throw new Exception(message);}
Check(!LauncherRenderPolicy.UseSoftware(Array.Empty<string>()),"Normal launch permits hardware acceleration.");
Check(!LauncherRenderPolicy.UseSoftware(new[]{"--play"}),"Direct game launch also permits hardware acceleration.");
Check(!LauncherRenderPolicy.UseSoftware(new[]{"--default-rendering"}),"The fallback must restore the earlier preference.");
Check(!LauncherRenderPolicy.UseSoftware(new[]{"--memory-audit","--default-rendering"}),"Default audit runs must not accidentally test software twice.");
Check(LauncherRenderPolicy.UseSoftware(new[]{"--memory-audit","--software-rendering"}),"Software audit runs must select software.");
Check(LauncherRenderPolicy.UseSoftware(new[]{"--default-rendering","--software-rendering"}),"Explicit software preference wins conflicting flags.");
Console.WriteLine("PASS: renderer default, fallback, direct play and distinct audit preferences.");
