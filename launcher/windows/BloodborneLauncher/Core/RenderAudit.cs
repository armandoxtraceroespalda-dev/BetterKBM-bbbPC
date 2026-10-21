using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace BloodborneLauncher.Core;

/// <summary>Opt-in, finite comparison of the same launcher under default and software WPF rendering.</summary>
internal static class RenderAudit
{
    public static bool Enabled => Environment.GetCommandLineArgs().Contains("--memory-audit");
    public static bool Software => LauncherRenderPolicy.UseSoftware(Environment.GetCommandLineArgs());
    public static string Mode => Software ? "software" : "default";
    public static void Start(Window window)
    {
        if (!Enabled) return;
        var samples = new List<object>();
        var started = Stopwatch.StartNew();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) =>
        {
            using var process = Process.GetCurrentProcess();
            samples.Add(new {
                seconds = Math.Round(started.Elapsed.TotalSeconds, 2),
                workingSetMiB = process.WorkingSet64 / 1048576.0,
                privateCommitMiB = process.PrivateMemorySize64 / 1048576.0,
                managedMiB = GC.GetTotalMemory(false) / 1048576.0,
                gcCommittedMiB = GC.GetGCMemoryInfo().TotalCommittedBytes / 1048576.0,
                cpuSeconds = process.TotalProcessorTime.TotalSeconds
            });
            if (started.Elapsed.TotalSeconds < 14) return;
            timer.Stop();
            try
            {
                // Module image sizes are mapped address-space sizes, not resident/private RAM.
                var modules = new List<object>();
                try
                {
                    foreach (ProcessModule module in process.Modules)
                        modules.Add(new { name = module.ModuleName, mappedImageMiB = module.ModuleMemorySize / 1048576.0 });
                }
                catch (System.ComponentModel.Win32Exception) { }
                var report = new {
                    mode = Mode, renderPreference = RenderOptions.ProcessRenderMode.ToString(),
                    renderTier = RenderCapability.Tier >> 16,
                    width = window.ActualWidth, height = window.ActualHeight,
                    dpi = VisualTreeHelper.GetDpi(window).DpiScaleX,
                    executable = Environment.ProcessPath,
                    samples, modules,
                    note = "Working set includes shared pages. Private commit is not private resident RAM. Module image sizes are not RAM attribution. No working-set trim or forced audit GC was used."
                };
                string path = Path.Combine(Paths.ConfigDir, "memory-audit-" + Mode + ".json");
                Directory.CreateDirectory(Paths.ConfigDir);
                File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
                LauncherDiagnostics.Memory("Render audit finished: " + Mode);
                window.Close();
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                LauncherDiagnostics.Record("Render audit failed: " + error.Message, LogLevel.Bad);
                Application.Current.Shutdown(1);
            }
        };
        window.Closed += (_, _) => timer.Stop();
        timer.Start();
    }
}
