namespace BloodborneLauncher.Core;

/// <summary>One decision per settled navigation batch, never a periodic/input-driven collection loop.</summary>
internal sealed class NavigationMemoryPolicy
{
    private const long MiB=1024*1024;
    internal const int SettleMilliseconds=400;
    internal const int CooldownMilliseconds=4000;
    internal const int NoProgressMilliseconds=30000;
    private long _queuedAt,_eligibleAt=long.MinValue/2;
    internal bool Pending {get;private set;}
    internal void Queue(long now) { _queuedAt=now;Pending=true; }
    internal void Cancel() => Pending=false;
    internal int Delay(long now) => !Pending ? -1 : (int)Math.Min(int.MaxValue,
        Math.Max(0,Math.Max(_queuedAt+SettleMilliseconds,_eligibleAt)-now));
    internal static bool ShouldRelease(long managed,long committed,int retired) =>
        (retired>0 && managed>=8*MiB) ||
        (managed>=0 && committed>=24*MiB && committed-managed>=12*MiB && managed<=committed/2);
    internal bool TryBegin(long now,bool busy,long managed,long committed,int retired,out int delay)
    {
        delay=Delay(now);
        if(delay<0)return false;
        // Skip this batch rather than polling indefinitely through a dialog/capture/drag.
        if(busy) { Cancel();delay=-1;return false; }
        if(delay>0)return false;
        Cancel();return ShouldRelease(managed,committed,retired);
    }
    internal void Completed(long now,long managedBefore,long committedBefore,long managedAfter,long committedAfter)
    {
        bool useful=managedBefore-managedAfter>=MiB || committedBefore-committedAfter>=MiB;
        _eligibleAt=now+(useful?CooldownMilliseconds:NoProgressMilliseconds);
    }
}
