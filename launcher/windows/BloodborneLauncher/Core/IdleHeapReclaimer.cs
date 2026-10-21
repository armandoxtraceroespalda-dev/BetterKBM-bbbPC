// Historical experiment, excluded from the launcher project since UI31.
// Normal operation uses explicit resource ownership and the runtime GC.
using System.Diagnostics;
using System.Runtime;

namespace BloodborneLauncher.Core;

/// <summary>Reclaim discarded pages after transitions, without trimming resident pages.</summary>
internal sealed class IdleHeapReclaimer
{
    internal const int QuietMilliseconds = 5000;
    internal const int CooldownMilliseconds = 30000;
    internal const int NavigationDebounceMilliseconds = 200;
    internal const int NavigationCooldownMilliseconds = 750;
    private const long MiB = 1024 * 1024;
    private bool _pending;
    public bool NavigationPending { get; private set; }
    private long _lastActivity;
    private long _lastRelease = long.MinValue / 2;

    public void Queue(long now)
    {
        // A delayed settings save must not downgrade an already queued navigation cleanup.
        if (_pending && NavigationPending) return;
        _pending = true; NavigationPending = false; _lastActivity = now;
    }
    public void QueueNavigation(long now)
    {
        _pending = true; NavigationPending = true; _lastActivity = now;
    }
    public void NoteInteraction(long now)
    {
        // Hovering the new page must not postpone its post-transition cleanup indefinitely.
        if (_pending && !NavigationPending) _lastActivity = now;
    }
    public void Cancel() { _pending = false; NavigationPending = false; }

    // A collected view is not required: the reported excess remained after every retired view died.
    public static bool HasExcess(long committed, long managed) =>
        managed >= 0 && committed >= 32 * MiB && committed - managed >= 16 * MiB &&
        managed <= committed / 2;

    public static bool ShouldRelease(long committed, long managed, bool navigation, int retiredViews) =>
        HasExcess(committed, managed) || (navigation && retiredViews > 0 && managed >= 8 * MiB);

    public int DelayBeforeCheck(long now, bool busy)
    {
        if (!_pending) return -1;
        int quiet = NavigationPending ? NavigationDebounceMilliseconds : QuietMilliseconds;
        int cooldown = NavigationPending ? NavigationCooldownMilliseconds : CooldownMilliseconds;
        long delay = Math.Max(0, Math.Max(quiet - (now - _lastActivity), cooldown - (now - _lastRelease)));
        return (int)Math.Max(busy ? (NavigationPending ? 100 : 1000) : 0, delay);
    }

    public bool TryRelease(long now, bool busy, out ReleaseResult result, out int retryAfterMs, int retiredViews = 0)
    {
        result = default;
        retryAfterMs = DelayBeforeCheck(now, busy);
        if (retryAfterMs != 0) return false;
        // Check once per settled navigation batch, not on a repeating idle poll.
        bool navigation = NavigationPending;
        Cancel();
        long committed = GC.GetGCMemoryInfo().TotalCommittedBytes;
        long managed = GC.GetTotalMemory(false);
        if (!ShouldRelease(committed, managed, navigation, retiredViews)) return false;
        _lastRelease = now;
        result = Release(committed, managed);
        return true;
    }

    internal static ReleaseResult Release(long committedBefore, long managedBefore)
    {
        long start = Stopwatch.GetTimestamp();
        // Aggressive requires a full blocking, compacting collection in .NET 8. CompactOnce
        // also covers discarded large buffers. Navigation calls wait for the finished transition
        // and a short debounce; settings-only calls retain the longer interaction quiet period.
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        return new(committedBefore, GC.GetGCMemoryInfo().TotalCommittedBytes,
            managedBefore, GC.GetTotalMemory(false), Stopwatch.GetElapsedTime(start).TotalMilliseconds);
    }

    internal readonly record struct ReleaseResult(long CommittedBefore, long CommittedAfter,
        long ManagedBefore, long ManagedAfter, double ElapsedMilliseconds);
}
