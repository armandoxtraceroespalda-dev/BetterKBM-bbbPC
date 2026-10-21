using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace BloodborneLauncher.Core;

/// <summary>Opt-in observations only. Never collect, trim, retain old views or hook pointer movement.</summary>
internal sealed class RetentionAudit : IDisposable
{
    internal static bool Enabled { get; } = Environment.GetCommandLineArgs().Contains("--retention-audit");
    private readonly Window _owner;
    private readonly Func<string> _active;
    private readonly Func<int> _cached;
    private readonly DispatcherTimer _timer;
    private readonly Process _process=Process.GetCurrentProcess();
    private readonly StreamWriter _output;
    private readonly Stopwatch _elapsed=Stopwatch.StartNew();
    private string _stage="startup";
    private bool _disposed;
    private RetentionAudit(Window owner,Func<string> active,Func<int> cached)
    {
        _owner=owner;_active=active;_cached=cached;
        Directory.CreateDirectory(Paths.ConfigDir);
        string path=Path.Combine(Paths.ConfigDir,"launcher-retention.csv");
        _output=new StreamWriter(path,false) { AutoFlush=true };
        _output.WriteLine("seconds,stage,active,cached,views_created,views_alive,retired_alive,working_set_bytes,private_bytes,managed_bytes,gc_committed_bytes,allocated_bytes,gen0,gen1,gen2,handles,threads,cached_pixel_bytes_estimate,wave_cache_bytes,navigation_cleanup_enabled,view_types");
        _timer=new DispatcherTimer(DispatcherPriority.Background) {Interval=TimeSpan.FromSeconds(5)};
        _timer.Tick+=Sample;
        _owner.IsVisibleChanged+=VisibilityChanged;
        Sample(null,EventArgs.Empty);if(owner.IsVisible)_timer.Start();
        LauncherDiagnostics.Record("Retention audit: "+path+"; sampler does not collect or trim; navigation policy follows --automatic-gc-only.",LogLevel.Info);
    }
    internal static RetentionAudit? Start(Window owner,Func<string> active,Func<int> cached)
    {
        if(!Enabled)return null;
        try { return new(owner,active,cached); }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        { LauncherDiagnostics.Record("Retention audit unavailable: "+error.Message,LogLevel.Warn);return null; }
    }
    internal void Mark(string stage) => _stage=stage;
    private void VisibilityChanged(object sender,DependencyPropertyChangedEventArgs args)
    { if(_disposed)return;if(_owner.IsVisible)_timer.Start();else _timer.Stop(); }
    private void Sample(object? sender,EventArgs args)
    {
        if(_disposed)return;
        try
        {
            _process.Refresh();var memory=GC.GetGCMemoryInfo();var views=ViewRetention.Snapshot();
            var row=new object[] {_elapsed.Elapsed.TotalSeconds.ToString("0.0",CultureInfo.InvariantCulture),_stage,_active(),_cached(),
                views.Created,views.Alive,views.RetiredAlive,_process.WorkingSet64,_process.PrivateMemorySize64,
                GC.GetTotalMemory(false),memory.TotalCommittedBytes,GC.GetTotalAllocatedBytes(false),
                GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2),_process.HandleCount,_process.Threads.Count,
                MenuAssets.RetainedPixelBytes,MenuSounds.ResidentWaveBytes,NavigationMemoryCoordinator.Enabled,ViewRetention.DescribeAlive()};
            _output.WriteLine(string.Join(",",row.Select(value=>"\""+Convert.ToString(value,CultureInfo.InvariantCulture)!.Replace("\"","\"\"")+"\"")));
            _stage="steady";
        }
        catch(Exception error) when(error is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        { LauncherDiagnostics.Record("Retention sample unavailable: "+error.Message,LogLevel.Warn); }
    }
    public void Dispose()
    {
        if(_disposed)return;_disposed=true;
        _timer.Stop();_timer.Tick-=Sample;_owner.IsVisibleChanged-=VisibilityChanged;
        _output.Dispose();_process.Dispose();
    }
}
