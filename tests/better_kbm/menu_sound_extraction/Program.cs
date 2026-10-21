using BloodborneLauncher.Core;
using System.Security.Cryptography;

if(args.FirstOrDefault()==MenuSoundExtractionProcess.WorkerArgument)
{
    try {GameMenuSoundExtractor.Extract(args[1],args[2],new AssetExtractionProcess.ProgressWriter());return 0;}
    catch(Exception error){Console.Error.WriteLine(error.Message);return 1;}
}
if(args.Length>1 && args[0]=="--real")
{
    string output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
    Dictionary<string,string>? baseline=null;
    foreach(var bank in args.Skip(2))
    {
        string root=Path.Combine(output,Path.GetFileNameWithoutExtension(bank));
        string sound=Path.Combine(root,"dvdroot_ps4/sound");Directory.CreateDirectory(sound);
        File.Copy(bank,Path.Combine(sound,Path.GetFileName(bank)),overwrite:true);
        var clock=System.Diagnostics.Stopwatch.StartNew();
        GameMenuSoundExtractor.Extract(root,Path.Combine(root,"gui"));
        var hashes=GameMenuSoundExtractor.Samples.Keys.ToDictionary(name=>name,name=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,"gui/sounds",name)))));
        if(baseline!=null && hashes.Any(pair=>baseline[pair.Key]!=pair.Value))throw new Exception("Regional menu audio differs unexpectedly.");
        baseline=hashes;
        Console.WriteLine(Path.GetFileName(bank)+" "+clock.ElapsedMilliseconds+" ms: "+string.Join(", ",hashes.Select(p=>p.Key+"="+p.Value[..12])));
    }
    Console.WriteLine("PASS: real ATRAC9 banks decode to identical PCM menu clips across regions.");return 0;
}
var sandbox=Path.Combine(Path.GetTempPath(),"bb-menu-sounds-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(sandbox);
try
{
    var game=Path.Combine(sandbox,"game");var sound=Path.Combine(game,"dvdroot_ps4/sound");Directory.CreateDirectory(sound);
    var bank=Path.Combine(sound,"sprj_main_eng.fsb");MakeBank(bank);
    var sourceHash=Hash(bank);var output=Path.Combine(sandbox,"gui");
    Check(!GameMenuSoundExtractor.IsReady(output),"An empty cache requires extraction.");
    GameMenuSoundExtractor.Extract(game,output);
    Check(GameMenuSoundExtractor.IsReady(output) && Hash(bank)==sourceHash,"Extraction must leave the source bank unchanged.");
    var target=GameMenuSoundExtractor.SoundDirectory(output);
    File.WriteAllText(Path.Combine(target,"select.wav"),"legacy custom confirmation");
    var saved=GameMenuSoundExtractor.Samples.Keys.ToDictionary(n=>n,n=>(Hash(Path.Combine(target,n)),File.GetLastWriteTimeUtc(Path.Combine(target,n))));
    File.Move(bank,bank+".away");GameMenuSoundExtractor.Extract(game,output);File.Move(bank+".away",bank);
    Check(saved.All(p=>p.Value.Item2==File.GetLastWriteTimeUtc(Path.Combine(target,p.Key))),"Warm caches must not reopen a missing source bank or rewrite clips.");
    var custom=Path.Combine(target,"hover.wav");var bytes=File.ReadAllBytes(custom);bytes[44]=77;File.WriteAllBytes(custom,bytes);var customHash=Hash(custom);
    File.WriteAllText(Path.Combine(target,"confirm.wav"),"invalid");GameMenuSoundExtractor.Extract(game,output);
    Check(Hash(custom)==customHash && Hash(Path.Combine(target,"confirm.wav"))==saved["confirm.wav"].Item1,"Repair must preserve valid user clips and replace only invalid clips.");
    Check(File.ReadAllText(Path.Combine(target,"select.wav"))=="legacy custom confirmation","Legacy custom clips must remain untouched during the confirmation upgrade.");
    var alternate=Path.Combine(sound,"sprj_main_ese.fsb");File.Copy(bank,alternate);File.WriteAllBytes(bank,new byte[]{1,2,3});
    GameMenuSoundExtractor.Extract(game,Path.Combine(sandbox,"fallback"));
    Check(GameMenuSoundExtractor.IsReady(Path.Combine(sandbox,"fallback")),"An invalid preferred bank must fall back to another language.");
    File.Copy(alternate,bank,true);
    using(var cancelled=new CancellationTokenSource())
    {
        cancelled.Cancel();bool hit=false;
        try{GameMenuSoundExtractor.Extract(game,Path.Combine(sandbox,"cancelled"),cancel:cancelled.Token);}catch(OperationCanceledException){hit=true;}
        Check(hit && !Directory.Exists(Path.Combine(sandbox,"cancelled")),"Pre-cancelled extraction must not create output.");
    }
    var workerGui=Path.Combine(sandbox,"worker-gui");Directory.CreateDirectory(Path.Combine(workerGui,"sounds"));
    File.Copy(custom,Path.Combine(workerGui,"sounds/hover.wav"));
    await MenuSoundExtractionProcess.ExtractAsync(game,workerGui,new Progress<GameAssetExtractor.Step>(),CancellationToken.None);
    Check(GameMenuSoundExtractor.IsReady(workerGui) && Hash(Path.Combine(workerGui,"sounds/hover.wav"))==customHash,"A real isolated worker must keep user clips and install the other effects.");
    Check(!Directory.EnumerateDirectories(workerGui,".sound-worker-*").Any(),"A successful worker must clean up staging.");
    using(var cancelWorker=new CancellationTokenSource())
    {
        var cancelGui=Path.Combine(sandbox,"cancel-worker");Directory.CreateDirectory(Path.Combine(cancelGui,"sounds"));File.Copy(custom,Path.Combine(cancelGui,"sounds/hover.wav"));
        bool hit=false;
        try{await MenuSoundExtractionProcess.ExtractAsync(game,cancelGui,new CancelProgress(cancelWorker),cancelWorker.Token);}catch(OperationCanceledException){hit=true;}
        Check(hit && Hash(Path.Combine(cancelGui,"sounds/hover.wav"))==customHash && !Directory.EnumerateDirectories(cancelGui,".sound-worker-*").Any(),"Cancelling a real sound worker must preserve existing clips and remove staging.");
    }
    var badGame=Path.Combine(sandbox,"bad-game");Directory.CreateDirectory(Path.Combine(badGame,"dvdroot_ps4/sound"));
    File.WriteAllBytes(Path.Combine(badGame,"dvdroot_ps4/sound/sprj_main_eng.fsb"),new byte[]{1,2,3});
    var failedGui=Path.Combine(sandbox,"failed-gui");Directory.CreateDirectory(Path.Combine(failedGui,"sounds"));File.Copy(custom,Path.Combine(failedGui,"sounds/hover.wav"));
    bool failed=false;
    try{await MenuSoundExtractionProcess.ExtractAsync(badGame,failedGui,new Progress<GameAssetExtractor.Step>(),CancellationToken.None);}catch(IOException){failed=true;}
    Check(failed && Hash(Path.Combine(failedGui,"sounds/hover.wav"))==customHash && !Directory.EnumerateDirectories(failedGui,".sound-worker-*").Any(),"Failed workers must preserve user clips and remove staging.");
    Console.WriteLine("PASS: FSB parsing, original-file preservation, PCM extraction, warm cache, custom WAV preservation, repair, bank fallback, cancellation and real isolated worker lifecycle.");
}
finally{Directory.Delete(sandbox,true);}
return 0;

static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
static void MakeBank(string path)
{
    var names=GameMenuSoundExtractor.Samples.Values.ToArray();int count=names.Length,frames=9600,bytesPerClip=frames*2;
    using var strings=new MemoryStream();using var namesWriter=new BinaryWriter(strings);
    for(int i=0;i<count;i++)namesWriter.Write(0);
    var offsets=new int[count];
    for(int i=0;i<count;i++){offsets[i]=(int)strings.Position;namesWriter.Write(System.Text.Encoding.UTF8.GetBytes(names[i]+"\0"));}
    strings.Position=0;foreach(var offset in offsets)namesWriter.Write(offset);
    using var output=File.Create(path);using var writer=new BinaryWriter(output);
    var header=new byte[60];"FSB5"u8.CopyTo(header);BitConverter.GetBytes(1).CopyTo(header,4);BitConverter.GetBytes(count).CopyTo(header,8);
    BitConverter.GetBytes(count*8).CopyTo(header,12);BitConverter.GetBytes((int)strings.Length).CopyTo(header,16);BitConverter.GetBytes(count*bytesPerClip).CopyTo(header,20);BitConverter.GetBytes(2).CopyTo(header,24);writer.Write(header);
    for(int i=0;i<count;i++)writer.Write(((ulong)frames<<34)|((ulong)(i*bytesPerClip/32)<<7)|(9UL<<1));
    writer.Write(strings.ToArray());
    for(int i=0;i<count;i++)for(int sample=0;sample<frames;sample++)writer.Write((short)(100+i+sample%100));
}


sealed class CancelProgress(CancellationTokenSource cancel) : IProgress<GameAssetExtractor.Step>
{ public void Report(GameAssetExtractor.Step step) => cancel.Cancel(); }

namespace BloodborneLauncher.Core
{
    public static class GameAssetExtractor
    {
        public readonly record struct Step(int Percent,string Message);
        public static readonly string[] RequiredFiles=Array.Empty<string>();
        public static bool IsReady(string path)=>false;
    }
}
