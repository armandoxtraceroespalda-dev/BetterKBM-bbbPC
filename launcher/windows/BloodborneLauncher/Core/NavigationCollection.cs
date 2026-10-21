using System.Diagnostics;
using System.Runtime;
namespace BloodborneLauncher.Core;

/// <summary>The sole induced-collection site, gated by settled navigation and measured managed pressure.</summary>
internal static class NavigationCollection
{
    internal static (long Managed,long Committed,double Milliseconds) Release()
    {
        long start=Stopwatch.GetTimestamp();
        GCSettings.LargeObjectHeapCompactionMode=GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration,GCCollectionMode.Aggressive,blocking:true,compacting:true);
        return(GC.GetTotalMemory(false),GC.GetGCMemoryInfo().TotalCommittedBytes,Stopwatch.GetElapsedTime(start).TotalMilliseconds);
    }
}
