using BloodborneLauncher.Core;
using System.Runtime.CompilerServices;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var (stream,reference)=ImportFixture.Create();
using(stream)
{
 Check(stream.RetainedBytes==1024*1024 && stream.CanRead && stream.CanSeek,"PCM import starts with one readable staging array.");
 byte[] cache=new byte[stream.Length];stream.ReadExactly(cache);
 Check(cache[0]==7 && cache[^1]==11,"Import must preserve every sample.");
 Check(stream.Seek(0,SeekOrigin.End)==cache.Length && stream.Read(new byte[1])==0,"The import supports ordinary stream EOF.");
 stream.ReleaseSamples();stream.ReleaseSamples();
 GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
 Check(stream.RetainedBytes==0 && !reference.TryGetTarget(out _) && !stream.CanRead,"Loaded audio must not retain the original PCM array through its Stream.");
 Check(cache[0]==7 && cache[^1]==11,"Releasing the staging array must preserve the playback cache.");
 try{stream.Read(new byte[1]);throw new Exception("A released import cannot be reread.");}catch(ObjectDisposedException){}
}
int released=0;
var lifetime=new PageLifetime();lifetime.Add(()=>throw new IOException("Fixture cleanup failure"));lifetime.Add(()=>released++);
try{lifetime.Dispose();throw new Exception("Cleanup failure must be reported.");}catch(AggregateException error){Check(error.InnerExceptions.Count==1,"Report the failed cleanup only once.");}
lifetime.Dispose();Check(released==1,"A failed cleanup cannot leave later subscriptions/timers active.");
var cacheViews=new PageCache<int,int>(3,value=>{released++;if(value==1)throw new IOException("Eviction failure");});
for(int value=1;value<=3;value++)cacheViews.GetOrCreate(value,()=>value);
try{cacheViews.Clear();throw new Exception("Cache cleanup failure must be reported.");}catch(AggregateException){}
Check(released==4 && cacheViews.Count==0,"Clear must retire all cached resources even if one disposal fails.");cacheViews.Dispose();
Console.WriteLine("PASS: PCM sample ownership releases redundant arrays; failed disposal still retires all remaining resources.");
static class ImportFixture
{
 [MethodImpl(MethodImplOptions.NoInlining)]
 internal static (WaveLoadStream,WeakReference<byte[]>) Create()
 {var wave=new byte[1024*1024];wave[0]=7;wave[^1]=11;return(new(wave),new(wave));}
}
