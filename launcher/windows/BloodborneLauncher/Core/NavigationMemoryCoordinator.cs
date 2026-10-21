using System.Windows;
using System.Windows.Threading;

namespace BloodborneLauncher.Core;

/// <summary>Own one post-transition attempt and its evidence; do not trim resident pages or hook pointer events.</summary>
internal sealed class NavigationMemoryCoordinator : IDisposable
{
    internal static bool Enabled {get;}=!Environment.GetCommandLineArgs().Any(arg=>arg is "--automatic-gc-only" or "--memory-audit");
    private readonly Window _owner;
    private readonly Func<bool> _busy;
    private readonly NavigationMemoryPolicy _policy=new();
    private readonly DispatcherTimer _timer;
    private bool _disposed;
    internal NavigationMemoryCoordinator(Window owner,Func<bool> busy)
    {
        _owner=owner;_busy=busy;
        _timer=new DispatcherTimer(DispatcherPriority.ApplicationIdle);
        _timer.Tick+=Check;
        _owner.IsVisibleChanged+=VisibilityChanged;
    }
    internal void Suspend() { _timer.Stop();_policy.Cancel(); }
    internal void Queue()
    {
        if(_disposed || !Enabled || !_owner.IsVisible)return;
        _timer.Stop();_policy.Queue(Environment.TickCount64);Arm(_policy.Delay(Environment.TickCount64));
    }
    private void Arm(int delay)
    {
        if(delay<0 || _disposed)return;
        _timer.Interval=TimeSpan.FromMilliseconds(Math.Max(1,delay));_timer.Start();
    }
    private void VisibilityChanged(object sender,DependencyPropertyChangedEventArgs args)
    { if(!_owner.IsVisible)Suspend(); }
    private void Check(object? sender,EventArgs args)
    {
        _timer.Stop();
        if(_disposed || !_owner.IsVisible) { _policy.Cancel();return; }
        if(_busy()) { _policy.Cancel();return; }
        long managed=GC.GetTotalMemory(false),committed=GC.GetGCMemoryInfo().TotalCommittedBytes;
        int retired=ViewRetention.Snapshot().RetiredAlive;
        if(!_policy.TryBegin(Environment.TickCount64,false,managed,committed,retired,out int delay))
        {
            if(delay>0)Arm(delay);
            else LauncherDiagnostics.Memory("UI32 navigation settled; no eligible managed cleanup");
            return;
        }
        LauncherDiagnostics.Memory("UI32 before navigation cleanup; retired="+retired);
        var result=NavigationCollection.Release();
        _policy.Completed(Environment.TickCount64,managed,committed,result.Managed,result.Committed);
        LauncherDiagnostics.Record($"UI32 navigation cleanup: managed={managed/1048576.0:0.0}->{result.Managed/1048576.0:0.0} MiB; GC committed={committed/1048576.0:0.0}->{result.Committed/1048576.0:0.0} MiB; pause={result.Milliseconds:0.0} ms. One settled navigation batch; no working-set trim.",LogLevel.Info);
        LauncherDiagnostics.Memory("UI32 after navigation cleanup");
        // No rearm here. Another actual navigation is required for any further attempt.
    }
    public void Dispose()
    {
        if(_disposed)return;_disposed=true;Suspend();_timer.Tick-=Check;
        _owner.IsVisibleChanged-=VisibilityChanged;
    }
}
