using System.Text;
using BloodborneLauncher.Core;
static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
var needle=Encoding.ASCII.GetBytes("trophy %d unlocked");
var root=Path.Combine(Path.GetTempPath(),"bb-stream-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
try
{
    string path=Path.Combine(root,"probe.bin");
    using(var file=File.Create(path)){file.SetLength(96L*1024*1024);file.Position=file.Length-needle.Length;file.Write(needle);}
    long before=GC.GetAllocatedBytesForCurrentThread();
    Check(TrophyTracker.RuntimeReportsTrophies(path),"Match at the end of a 96 MiB binary.");
    long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
    Check(allocated<1024*1024,$"Probe scan allocated {allocated} bytes instead of a bounded buffer.");
    foreach(int split in new[]{1,3,17,65530})
    {
        var bytes=new byte[65536+needle.Length+7];needle.CopyTo(bytes,65536-split%needle.Length);
        using var shortReads=new ShortReads(bytes,split);
        Check(StreamScan.Contains(shortReads,needle),"Needle across short reads and block boundaries.");
    }
    using(var absent=new MemoryStream(new byte[131072]))Check(!StreamScan.Contains(absent,needle),"Absent marker.");
    string text="á\r\n"+new string('a',70000)+"\nRuntime: trophy 4 unlocked\nunfinished";
    using(var input=new ShortReads(Encoding.UTF8.GetBytes(text),7))
    {
        var lines=new List<string>();long end=StreamScan.CompleteLines(input,lines.Add);
        Check(lines.Count==3 && lines[0]=="á\r" && lines[1].Length==70000 && lines[2].Contains("trophy 4"),"UTF8, CRLF and spanning lines.");
        Check(end==Encoding.UTF8.GetByteCount(text[..text.LastIndexOf("unfinished")]),"Incomplete-line byte offset.");
    }
    var tracker=new TrophyTracker();
    string log=Path.Combine(root,"last_run.log");
    File.WriteAllText(log,"Runtime: trophy 4 unlocked\nRuntime: trophy 5 unlocked");tracker.SetUserDir(root);
    Check(tracker.Unlocked.ContainsKey(4) && !tracker.Unlocked.ContainsKey(5),"Partial trophy line must remain pending.");
    File.AppendAllText(log,"\nRuntime: trophy 6 earned in this save\n");tracker.Poll(true);
    Check(tracker.Unlocked.ContainsKey(5) && tracker.Unlocked.ContainsKey(6),"Resume and imported-save trophies.");
    File.WriteAllText(log,"Runtime: trophy 7 unlocked\n");tracker.Poll(true);
    Check(tracker.Unlocked.ContainsKey(7),"Rewritten/truncated log detection.");
    tracker.Reset();tracker.Poll(true);Check(tracker.Unlocked.Count==0,"Reset must not reimport the old log.");
    Console.WriteLine($"PASS: 96 MiB probe scanned with {allocated:N0} allocated bytes; boundary/short reads, UTF8, incomplete lines, append, rewrite and trophy reset.");
}
finally{Directory.Delete(root,true);}
sealed class ShortReads:MemoryStream
{
    private readonly int _size;
    public ShortReads(byte[] bytes,int size):base(bytes){_size=size;}
    public override int Read(byte[] buffer,int offset,int count)=>base.Read(buffer,offset,Math.Min(count,_size));
}
