using System.Diagnostics;
using System.IO;
namespace BloodborneLauncher.Core;

/// <summary>Small local evidence file; no network upload and no forced memory trimming.</summary>
public static class LauncherDiagnostics
{
    public static bool DetailedEnabled { get; } = Environment.GetEnvironmentVariable("BB_LAUNCHER_UI_TRACE")=="1" ||
        Environment.GetEnvironmentVariable("BB_LAUNCHER_HOVER_TRACE")=="1" ||
        Environment.GetCommandLineArgs().Any(a=>a is "--retention-audit" or "--memory-audit");
    public static string LogPath=>Path.Combine(Paths.ConfigDir,"launcher-diagnostics.log");
    public static string PreviousLogPath=>Path.Combine(Paths.ConfigDir,"launcher-diagnostics.previous.log");
    public static void Start()
    {
        try
        {
            Directory.CreateDirectory(Paths.ConfigDir);
            if(File.Exists(LogPath))File.Move(LogPath,PreviousLogPath,overwrite:true);
            File.WriteAllText(LogPath,"");
        }
        catch(IOException) { }
        catch(UnauthorizedAccessException) { }
        Record("Bloodborne UI 32; executable="+(Environment.ProcessPath??"unknown")+"; base="+AppContext.BaseDirectory,LogLevel.Info);
        Memory("Startup");
    }
    public static void Record(string message,LogLevel level)
    {
        try
        {
            Directory.CreateDirectory(Paths.ConfigDir);
            if(File.Exists(LogPath) && new FileInfo(LogPath).Length>262144)File.WriteAllText(LogPath,"Log rotated.\n");
            File.AppendAllText(LogPath,$"{DateTime.Now:O} [{level}] {message}{Environment.NewLine}");
        }
        catch(IOException) { }
        catch(UnauthorizedAccessException) { }
    }
    public static void Memory(string stage)
    {
        try
        {
            using var process=Process.GetCurrentProcess();var gc=GC.GetGCMemoryInfo();var views=ViewRetention.Snapshot();
            Record($"{stage}: working set={process.WorkingSet64/1048576.0:0.0} MiB; private commit={process.PrivateMemorySize64/1048576.0:0.0} MiB; managed={GC.GetTotalMemory(false)/1048576.0:0.0} MiB; GC committed={gc.TotalCommittedBytes/1048576.0:0.0} MiB; last GC heap={gc.HeapSizeBytes/1048576.0:0.0} MiB; last GC fragmentation={gc.FragmentedBytes/1048576.0:0.0} MiB; allocated total={GC.GetTotalAllocatedBytes(false)/1048576.0:0.0} MiB; collections={GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}; views created={views.Created}; alive={views.Alive}; retired awaiting collection={views.RetiredAlive}",LogLevel.Info);
        }
        catch(System.ComponentModel.Win32Exception error){Record("Memory sample unavailable: "+error.Message,LogLevel.Warn);}
        catch(InvalidOperationException error){Record("Memory sample unavailable: "+error.Message,LogLevel.Warn);}
    }

    /// <summary>Process-wide allocation interval, including nested/background work; not retained size.</summary>
    public static IDisposable Measure(string stage)
    {
        if(!DetailedEnabled)return EmptyPhase.Instance;
        Memory("Begin " + stage);
        return new Phase(stage);
    }

    private sealed class EmptyPhase : IDisposable
    {
        internal static readonly EmptyPhase Instance=new();
        public void Dispose() { }
    }

    private sealed class Phase : IDisposable
    {
        private readonly string _stage;
        private readonly long _start = Stopwatch.GetTimestamp();
        private readonly long _allocated = GC.GetTotalAllocatedBytes(false);
        private int _disposed;
        public Phase(string stage) => _stage = stage;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            var elapsed = Stopwatch.GetElapsedTime(_start);
            var allocated = GC.GetTotalAllocatedBytes(false) - _allocated;
            Memory($"End {_stage}; elapsed={elapsed.TotalMilliseconds:0.0} ms; interval allocated={allocated/1048576.0:0.0} MiB");
        }
    }
}
