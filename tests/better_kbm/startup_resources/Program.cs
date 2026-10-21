using BloodborneLauncher.Core;
using System.Security.Cryptography;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
Check(BackdropResolutionPolicy.Width(900,620,1)==1280,"Small windows must decode a smaller backdrop.");
Check(BackdropResolutionPolicy.Width(1280,800,1)==1600,"UniformToFill must account for a taller window.");
Check(BackdropResolutionPolicy.Width(1280,800,1.5)==1920,"DPI must preserve native image detail.");
Check(BackdropResolutionPolicy.Width(3840,2160,2)==1920,"Decode must stay bounded by the source's native width.");
Check(BackdropResolutionPolicy.Width(double.NaN,800)==1280,"Invalid dimensions need a safe bootstrap size.");
byte[] Wave(int bytes=88000){using var s=new MemoryStream();using var w=new BinaryWriter(s);w.Write("RIFF"u8);w.Write(bytes+36);w.Write("WAVEfmt "u8);w.Write(16);w.Write((ushort)1);w.Write((ushort)1);w.Write(44100);w.Write(88200);w.Write((ushort)2);w.Write((ushort)16);w.Write("data"u8);w.Write(bytes);w.Write(new byte[bytes]);return s.ToArray();}
var wave=Wave();using(var input=new CountingStream(wave)){
 Check(MenuWave.TryDuration(input,out var duration)&&duration==998,"Streaming validation must preserve PCM duration.");
 Check(input.ReadBytes<=44,"PCM samples must never be read by the cache validator.");
 Check(MenuWave.TryDuration(wave,out var original)&&original==duration,"Stream/span validation must agree.");
}
for(int i=0;i<44;i++){
 var broken=(byte[])wave.Clone();broken[i]^=255;using var input=new CountingStream(broken);
 Check(MenuWave.TryDuration(input,out _)==MenuWave.TryDuration(broken,out _),"Streaming validation must reject the same corrupt headers: "+i);
}
var directory=Path.Combine(Path.GetTempPath(),"bb-font-test-"+Guid.NewGuid());
try{
 UiFontAssets.EnsureInstalled(directory);var path=Path.Combine(directory,"LauncherFallback.otf");
 Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).Equals(UiFontAssets.Hash,StringComparison.OrdinalIgnoreCase),"Published font and versioned extraction must match.");
 var time=File.GetLastWriteTimeUtc(path);UiFontAssets.EnsureInstalled(directory);
 Check(time==File.GetLastWriteTimeUtc(path),"Cached font must not be rewritten on each startup.");
}finally{Directory.Delete(directory,true);}
Console.WriteLine("PASS: DPI/crop-aware bounded backdrop, streaming PCM validation without sample reads, bundled fallback extraction and reuse.");
sealed class CountingStream(byte[] data):Stream {
 readonly MemoryStream inner=new(data);public long ReadBytes;
 public override bool CanRead=>true;public override bool CanWrite=>false;public override bool CanSeek=>true;
 public override long Length=>inner.Length;public override long Position {get=>inner.Position;set=>inner.Position=value;}
 public override int Read(Span<byte> buffer){int n=inner.Read(buffer);ReadBytes+=n;return n;}
 public override int Read(byte[] b,int o,int c){int n=inner.Read(b,o,c);ReadBytes+=n;return n;}
 public override long Seek(long value,SeekOrigin origin)=>inner.Seek(value,origin);
 public override void Flush(){}public override void SetLength(long value)=>throw new NotSupportedException();
 public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
 protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
}
