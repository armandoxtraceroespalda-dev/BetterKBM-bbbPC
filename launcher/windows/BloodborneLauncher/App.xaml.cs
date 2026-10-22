using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace BloodborneLauncher
{
    public partial class App : Application
    {
        private static readonly string CrashLog = Path.Combine(Core.Paths.ConfigDir, "crash.log");

        /// <summary>When this launcher runs outside a package and the settings name one with its own
        /// launcher, that launcher is started with the same arguments (once: BB_LAUNCHER_HANDOVER).
        /// A build from the source checkout keeps running itself.</summary>
        private static bool HandOverToPackage(string[] args)
        {
            try
            {
                if (Environment.GetEnvironmentVariable("BB_LAUNCHER_HANDOVER") == "1" ||
                    Core.Paths.DetectPortDir() != null || Core.Paths.SourceRoot != null ||
                    Environment.ProcessPath is not { } self || !File.Exists(Core.Paths.ConfigFile))
                    return false;
                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Core.Paths.ConfigFile));
                if (!doc.RootElement.TryGetProperty("port_dir", out var value) || value.ValueKind != System.Text.Json.JsonValueKind.String)
                    return false;
                var package = Core.Paths.Expand(value.GetString() ?? "");
                var launcher = Path.Combine(package, Path.GetFileName(self));
                if (!Core.Paths.IsPortDir(package) || !File.Exists(launcher) ||
                    string.Equals(Path.GetFullPath(launcher), Path.GetFullPath(self), StringComparison.OrdinalIgnoreCase))
                    return false;
                var start = new System.Diagnostics.ProcessStartInfo(launcher) { UseShellExecute = false, WorkingDirectory = package };
                foreach (var arg in args) start.ArgumentList.Add(arg);
                start.Environment["BB_LAUNCHER_HANDOVER"] = "1";
                System.Diagnostics.Process.Start(start)?.Dispose();
                return true;
            }
            catch (Exception)
            {
                return false; // runs itself, as before
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            base.OnStartup(e);
            if(e.Args.FirstOrDefault() is Core.AssetExtractionProcess.WorkerArgument or Core.MenuSoundExtractionProcess.WorkerArgument)
            {
                // No main window, prompts, workspace polling or shared diagnostic-log rotation in workers.
                try
                {
                    if(e.Args.Length!=3)throw new ArgumentException(BloodborneLauncher.Core.UiText.T("Expected game and output folders for the asset worker."));
                    if(e.Args[0]==Core.MenuSoundExtractionProcess.WorkerArgument)
                        await Task.Run(()=>Core.GameMenuSoundExtractor.Extract(e.Args[1],e.Args[2],new Core.AssetExtractionProcess.ProgressWriter()));
                    else
                        await Task.Run(()=>Core.GameAssetExtractor.Extract(e.Args[1],e.Args[2],new Core.AssetExtractionProcess.ProgressWriter()));
                    Shutdown(0);
                }
                catch(Exception error){Console.Error.WriteLine(error.GetBaseException().Message);Shutdown(1);}
                return;
            }
            if (HandOverToPackage(e.Args)) { Shutdown(); return; }
            System.Windows.Media.RenderOptions.ProcessRenderMode = Core.LauncherRenderPolicy.UseSoftware(e.Args)
                ? System.Windows.Interop.RenderMode.SoftwareOnly
                : System.Windows.Interop.RenderMode.Default;
            Core.LauncherDiagnostics.Start();
            Core.LauncherDiagnostics.Record("WPF rendering preference: " + System.Windows.Media.RenderOptions.ProcessRenderMode +
                "; audit=" + Core.RenderAudit.Enabled, Core.LogLevel.Info);

            // A UI-thread fault is recoverable; the other two are only logged, so a hard exit
            // still leaves evidence behind.
            DispatcherUnhandledException += OnDispatcherException;
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                Record("AppDomain", args.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                Record("UnobservedTask", args.Exception);
                args.SetObserved();
            };
            try
            {
                var settings = new Core.LauncherSettings();
                using (Core.LauncherDiagnostics.Measure("Startup settings")) settings.Load();
                Core.UiFontAssets.EnsureInstalled();
                Core.GameLanguages.Ensure(settings);
                Core.UiText.UseTextLanguage(Core.GameLanguages.Text(settings));
                bool assetsReady;
                using (Core.LauncherDiagnostics.Measure("Startup asset setup"))
                    assetsReady = await Views.GameAssetSetup.EnsureAsync(settings);
                if (!assetsReady) { Shutdown();return; }
                MainWindow window;
                using (Core.LauncherDiagnostics.Measure("Main window construction")) window = new MainWindow();
                MainWindow = window;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                using (Core.LauncherDiagnostics.Measure("Initial window show")) window.Show();
            }
            catch (Exception error)
            {
                Record("Startup", error);
                MessageBox.Show(BloodborneLauncher.Core.UiText.Format("{0}\n\nLogged to:\n{1}",error.GetBaseException().Message,CrashLog),
                    BloodborneLauncher.Core.UiText.T("Could not open Bloodborne Launcher"), MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }

        }

        private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            // A transient desktop-composition reset does not invalidate the custom shell.
            if(args.Exception is System.Runtime.InteropServices.COMException {HResult:unchecked((int)0x80263001)}) {
                args.Handled=true;
                return;
            }
            Record("Dispatcher", args.Exception);
            MessageBox.Show(BloodborneLauncher.Core.UiText.F($"{args.Exception.Message}\n\nLogged to:\n{CrashLog}"),
                BloodborneLauncher.Core.UiText.T("Unexpected error"), MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        }

        private static void Record(string source, Exception? exception)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CrashLog)!);
                var entry = new StringBuilder()
                    .AppendLine(new string('=', 70))
                    .AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  [{source}]")
                    .AppendLine(exception?.ToString() ?? "(no exception object)")
                    .AppendLine();
                File.AppendAllText(CrashLog, entry.ToString());
            }
            catch
            {
                // Logging must never be the thing that takes the app down.
            }
        }
    }
}
