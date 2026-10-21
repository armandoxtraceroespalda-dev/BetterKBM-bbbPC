using BloodborneLauncher.Core;
using System.Runtime.CompilerServices;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
const long MiB = 1024 * 1024;
Check(IdleHeapReclaimer.HasExcess((long)(59.4 * MiB), (long)(9.6 * MiB)), "The supplied settled UI21 sample must qualify even with no retired views.");
Check(!IdleHeapReclaimer.HasExcess((long)(7.1 * MiB), (long)(13.1 * MiB)), "The supplied startup sample must not qualify.");
Check(!IdleHeapReclaimer.HasExcess(31 * MiB, 4 * MiB), "Small committed heaps must not be compacted.");
Check(!IdleHeapReclaimer.HasExcess(40 * MiB, 32 * MiB), "Mostly live heaps must not be compacted.");
Check(!IdleHeapReclaimer.HasExcess(64 * MiB, 40 * MiB), "Large live working sets must not be mistaken for spare capacity.");
Check(IdleHeapReclaimer.ShouldRelease(16 * MiB, 14 * MiB, true, 1), "A discarded page must qualify even before its dead objects have been swept.");
Check(!IdleHeapReclaimer.ShouldRelease(16 * MiB, 14 * MiB, true, 0), "A live current page alone must not trigger unnecessary compaction.");
Check(!IdleHeapReclaimer.ShouldRelease(16 * MiB, 14 * MiB, false, 1), "Settings-only cleanup retains its conservative excess-commit threshold.");
Check(!IdleHeapReclaimer.ShouldRelease(4 * MiB, 3 * MiB, true, 1), "Tiny navigation heaps should be left to normal GC.");

var idle = new IdleHeapReclaimer();
Check(idle.DelayBeforeCheck(100, false) == -1, "Idle alone must never start a compaction loop.");
idle.Queue(100);
Check(idle.DelayBeforeCheck(5099, false) == 1, "Wait for the complete quiet interval.");
idle.NoteInteraction(5000);
Check(idle.DelayBeforeCheck(5099, false) == 4901, "Pointer/keyboard activity must postpone compaction.");
Check(idle.DelayBeforeCheck(10000, true) == 1000, "Captured input/modal UI must delay the check.");
idle.Cancel();
Check(idle.DelayBeforeCheck(20000, false) == -1, "Cancellation must clear pending work.");

var navigation = new IdleHeapReclaimer();
navigation.QueueNavigation(100);
Check(navigation.DelayBeforeCheck(299,false)==1,"Navigation must wait only for its short post-transition debounce.");
navigation.NoteInteraction(290);
Check(navigation.DelayBeforeCheck(300,false)==0,"Moving the pointer on the new tab must not restart a five-second wait.");
navigation.Queue(299);
Check(navigation.NavigationPending && navigation.DelayBeforeCheck(300,false)==0,"A delayed save must not downgrade pending navigation cleanup.");
navigation.QueueNavigation(250);
Check(navigation.DelayBeforeCheck(300,false)==150,"Quick page changes must coalesce into the last transition.");
Check(navigation.DelayBeforeCheck(450,true)==100,"Captured input/modal UI must defer without losing the navigation request.");
Check(navigation.DelayBeforeCheck(450,false)==0,"Releasing input must allow prompt cleanup.");
navigation.Cancel();
Check(!navigation.NavigationPending && navigation.DelayBeforeCheck(500,false)==-1,"Starting another transition cancels the old cleanup request.");

// A native runtime integration test, not a WPF/Windows memory benchmark. Leave live
// anchors between dead small arrays to reproduce spare commit after a sweeping GC.
var anchors = AllocateFragmentedHeap();
GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
long before = GC.GetGCMemoryInfo().TotalCommittedBytes;
long live = GC.GetTotalMemory(false);
Console.WriteLine($"Fixture before release: committed={before / (double)MiB:0.0} MiB; managed={live / (double)MiB:0.0} MiB");
Check(IdleHeapReclaimer.HasExcess(before, live), "The fragmentation fixture must reproduce excess committed memory.");
idle.Queue(20000);
Check(!idle.TryRelease(24999, false, out _, out int retry) && retry == 1, "A real heap release must respect interaction quiet time.");
Check(idle.TryRelease(25000, false, out var release, out _), "The production path must release a qualifying idle heap.");
Console.WriteLine($"Fixture after release: committed={release.CommittedAfter / (double)MiB:0.0} MiB; managed={release.ManagedAfter / (double)MiB:0.0} MiB; elapsed={release.ElapsedMilliseconds:0.0} ms");
Check(release.CommittedAfter < release.CommittedBefore, "Aggressive collection must actually decommit memory in this fixture.");
for (int i = 0; i < anchors.Length; i++) Check(anchors[i][0] == (byte)i, "Compaction must preserve live object contents.");
Check(idle.DelayBeforeCheck(26000, false) == -1, "A completed attempt must not repeat without new navigation.");
idle.Queue(30000);
Check(idle.DelayBeforeCheck(35000, false) == 20000, "Another navigation must respect the thirty-second release cooldown.");
Check(idle.DelayBeforeCheck(55000, false) == 0, "The next batch becomes eligible after the cooldown.");
idle.Cancel();

var retired=CreateRetiredPage();
navigation.QueueNavigation(60000);
Check(!navigation.TryRelease(60199,false,out _,out retry,1) && retry==1,"The production navigation path must respect its short debounce.");
Check(navigation.TryRelease(60200,false,out var navigationRelease,out _,1),"The production navigation path must collect a discarded page without a five-second idle wait.");
Check(!retired.TryGetTarget(out _),"The actual retired page fixture must be collected.");
for (int i = 0; i < anchors.Length; i++) Check(anchors[i][0] == (byte)i, "Navigation cleanup must preserve the active data.");
Check(navigation.DelayBeforeCheck(60300,false)==-1,"A completed navigation cleanup must not become a repeating timer.");
navigation.QueueNavigation(60300);
Check(navigation.DelayBeforeCheck(60500,false)==450,"Fast repeated navigation releases use a sub-second cooldown, not thirty seconds.");
Check(navigation.DelayBeforeCheck(60950,false)==0,"The next completed navigation is promptly eligible.");
navigation.Cancel();
Console.WriteLine($"Navigation release: committed={navigationRelease.CommittedBefore / (double)MiB:0.0} -> {navigationRelease.CommittedAfter / (double)MiB:0.0} MiB; managed={navigationRelease.ManagedBefore / (double)MiB:0.0} -> {navigationRelease.ManagedAfter / (double)MiB:0.0} MiB; elapsed={navigationRelease.ElapsedMilliseconds:0.0} ms");
GC.KeepAlive(anchors);
Console.WriteLine("PASS: supplied log thresholds, settings idle, prompt/coalesced post-transition navigation, hover/save priority, busy UI, cancellation, cooldown, actual decommit and retired-page collection with live data preserved.");

[MethodImpl(MethodImplOptions.NoInlining)]
static WeakReference<object> CreateRetiredPage()=>new(new byte[20 * 1024 * 1024]);

[MethodImpl(MethodImplOptions.NoInlining)]
static byte[][] AllocateFragmentedHeap()
{
    var buffers = new byte[8192][];
    for (int i = 0; i < buffers.Length; i++) { buffers[i] = new byte[16 * 1024]; buffers[i][0] = (byte)(i / 16); }
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
    var anchors = new byte[buffers.Length / 16][];
    for (int i = 0; i < anchors.Length; i++) anchors[i] = buffers[i * 16];
    return anchors;
}
