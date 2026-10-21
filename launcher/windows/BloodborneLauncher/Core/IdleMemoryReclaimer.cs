// Historical experiment, excluded from the launcher project since UI31.
// Normal operation uses explicit resource ownership and the runtime GC.
namespace BloodborneLauncher.Core;

/// <summary>Reclaim retired WPF trees after navigation settles; never trim the Windows working set.</summary>
internal static class IdleMemoryReclaimer
{
    private static long _lastCollection = long.MinValue / 2;
    public static bool ShouldCollect(long now, long managedBytes, int retiredViews, long lastCollection) =>
        retiredViews > 0 && managedBytes >= 8L * 1024 * 1024 && now - lastCollection >= 8000;
    public static int RetryAfter(long now, long managedBytes, int retiredViews, long lastCollection) =>
        retiredViews > 0 && managedBytes >= 8L * 1024 * 1024
            ? (int)Math.Max(0, 8000 - (now - lastCollection)) : 0;
    public static bool TryCollect(out int retryAfterMs)
    {
        long now = Environment.TickCount64;
        long bytes = GC.GetTotalMemory(false);
        int retired = ViewRetention.Snapshot().RetiredAlive;
        retryAfterMs = RetryAfter(now, bytes, retired, _lastCollection);
        if (!ShouldCollect(now, bytes, retired, _lastCollection)) return false;
        _lastCollection = now;
        // Background collection is requested only at idle, with dead page trees and a cooldown.
        GC.Collect(2, GCCollectionMode.Forced, blocking: false, compacting: false);
        return true;
    }
}
