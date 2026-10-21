// Historical experiment, excluded from the launcher project since UI31.
// Normal operation uses explicit resource ownership and the runtime GC.
namespace BloodborneLauncher.Core;

/// <summary>Bound temporary managed accumulation with periodic checks, independent of pointer pauses or tab changes.</summary>
internal sealed class InteractionHeapReclaimer
{
    private const long MiB = 1024 * 1024;
    internal const int ProbeMilliseconds = 500;
    internal const int QuietMilliseconds = 125;
    internal const int MaximumBurstMilliseconds = 500;
    internal const int CooldownMilliseconds = 1000;
    private long _managedBaseline, _allocatedBaseline, _lastInput, _burstStart;
    private long _nextRelease = long.MinValue / 2;
    public bool Tracking { get; private set; }

    public void Reset(long managed, long allocated)
    {
        _managedBaseline = managed; _allocatedBaseline = allocated; Tracking = false;
    }
    public void NoteInteraction(long now)
    {
        if (!Tracking) _burstStart = now;
        _lastInput = now; Tracking = true;
    }
    public void Cancel() => Tracking = false;
    public void BeginMaintenance()
    {
        if (Tracking) return;
        // Idle maintenance is not pointer input and must not start another quiet-time wait.
        _lastInput = _burstStart = long.MinValue / 2; Tracking = true;
    }

    internal static bool HasPressure(long managed, long committed, long allocated,
        long baselineManaged, long baselineAllocated) =>
        (managed >= 0 && committed >= 8 * MiB && committed - managed >= 4 * MiB) ||
        (managed - baselineManaged >= MiB / 2 && allocated - baselineAllocated >= MiB);

    // -1 ends this burst, 0 permits collection, positive values defer a single timer.
    public int DelayBeforeRelease(long now, bool busy, long managed, long committed, long allocated)
    {
        if (!Tracking) return -1;
        if (busy) return ProbeMilliseconds;
        if (managed < _managedBaseline) { _managedBaseline = managed; _allocatedBaseline = allocated; }
        if (!HasPressure(managed, committed, allocated, _managedBaseline, _allocatedBaseline))
        {
            if (now - _lastInput < ProbeMilliseconds) return ProbeMilliseconds;
            Tracking = false; return -1;
        }
        if (now < _nextRelease) return (int)Math.Min(int.MaxValue, _nextRelease - now);
        // Prefer a brief pause, but never postpone a large burst indefinitely while hovering.
        if (now - _lastInput < QuietMilliseconds && now - _burstStart < MaximumBurstMilliseconds)
            return QuietMilliseconds;
        return 0;
    }
    public void Released(long now, long committedBefore, long committedAfter, long managedAfter, long allocated)
    {
        // Back off if collection could not return useful commit; live page/resources may dominate.
        _nextRelease = now + (committedBefore - committedAfter >= MiB ? CooldownMilliseconds : 2000);
        Reset(managedAfter, allocated);
    }
}
