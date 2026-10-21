using BloodborneLauncher.Core;
using System.Runtime.CompilerServices;
static void Check(bool value,string message){if(!value)throw new Exception(message);}
const long MiB=1024*1024;
var policy=new NavigationMemoryPolicy();
Check(policy.Delay(100)==-1,"Idle or pointer movement alone cannot queue a collection.");
policy.Queue(100);Check(policy.Delay(499)==1,"Wait for the settled transition.");
policy.Queue(400);Check(policy.Delay(500)==300,"Rapid navigation coalesces into the last page.");
Check(!policy.TryBegin(800,true,16*MiB,24*MiB,1,out _) && !policy.Pending,"Modal/capture/drag skips the batch instead of busy polling.");
Check(!policy.TryBegin(5000,false,16*MiB,24*MiB,1,out _),"A skipped batch cannot restart on its own.");
Check(!NavigationMemoryPolicy.ShouldRelease(4*MiB,8*MiB,3),"Small heaps remain automatic.");
Check(!NavigationMemoryPolicy.ShouldRelease(40*MiB,48*MiB,0),"Live current content or native RAM is not spare managed commit.");
Check(NavigationMemoryPolicy.ShouldRelease(14*MiB,16*MiB,1),"Retired unswept pages qualify.");
Check(NavigationMemoryPolicy.ShouldRelease(8*MiB,32*MiB,0),"Spare managed commit can qualify after retired weak targets have died.");
policy.Queue(1000);Check(policy.TryBegin(1400,false,14*MiB,24*MiB,1,out _),"One settled batch may begin.");
Check(!policy.Pending && !policy.TryBegin(2000,false,14*MiB,24*MiB,1,out _),"There is no periodic rearm after an attempt.");
policy.Completed(1400,14*MiB,24*MiB,6*MiB,8*MiB);policy.Queue(2000);
Check(policy.Delay(2400)==3000,"Useful releases still have a four-second navigation cooldown.");
policy.Completed(5400,8*MiB,12*MiB,8*MiB,12*MiB);policy.Queue(5500);
Check(policy.Delay(6000)==29400,"Unproductive collections back off for thirty seconds.");
policy.Cancel();Check(policy.Delay(60000)==-1,"Hiding/closing or a new transition cancels pending work.");
// Exercise the actual production collection primitive against discarded buffers and live anchors.
var anchors=Fragment();
GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,blocking:true,compacting:false);
long before=GC.GetGCMemoryInfo().TotalCommittedBytes;
var result=NavigationCollection.Release();
Console.WriteLine($"Fixture GC commit: {before/(double)MiB:0.0} -> {result.Committed/(double)MiB:0.0} MiB; live={result.Managed/(double)MiB:0.0} MiB; pause={result.Milliseconds:0.0} ms.");
Check(result.Committed<before,"The production call must release actual excess commit in this controlled fixture.");
for(int index=0;index<anchors.Length;index++)Check(anchors[index][0]==(byte)index,"Live settings/current-view data remains intact.");
GC.KeepAlive(anchors);
Console.WriteLine("PASS: settled/coalesced navigation, busy skip, no idle/input loop, live-heap safeguards, cooldown/backoff, real managed decommit.");
[MethodImpl(MethodImplOptions.NoInlining)]
static byte[][] Fragment()
{
 var buffers=new byte[8192][];
 for(int i=0;i<buffers.Length;i++) { buffers[i]=new byte[16*1024];buffers[i][0]=(byte)(i/16); }
 GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,blocking:true,compacting:false);
 var live=new byte[buffers.Length/16][];
 for(int i=0;i<live.Length;i++)live[i]=buffers[i*16];
 return live;
}
