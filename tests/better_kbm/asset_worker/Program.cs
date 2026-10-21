using System.Diagnostics;
using System.Text.Json;
using BloodborneLauncher.Core;

if(args.FirstOrDefault()==AssetExtractionProcess.WorkerArgument)
{
    string game=args[1],output=args[2];
    if(game=="fail"){Console.Error.WriteLine("Fixture extraction failed");return 7;}
    var writer=new AssetExtractionProcess.ProgressWriter();
    writer.Report(new(5,game));
    if(game=="hang")await Task.Delay(TimeSpan.FromSeconds(20));
    if(game!="missing")foreach(var name in GameAssetExtractor.RequiredFiles.Append("assets-manifest.json"))File.WriteAllText(Path.Combine(output,name),"worker result");
    writer.Report(new(100,"Ready"));return 0;
}
var root=Path.Combine(Path.GetTempPath(),"bb worker "+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
try
{
    var received=new List<GameAssetExtractor.Step>();
    var progress=new Capture(step=>received.Add(step));
    string game="C:\\game folder with spaces\\Bloodborne";
    await AssetExtractionProcess.ExtractAsync(game,root,progress,CancellationToken.None);
    Check(received.First().Message==game && received.Last().Percent==100,"Arguments/progress must survive spaces and backslashes.");
    foreach(var mode in new[]{"fail","missing"})
    {
        try{await AssetExtractionProcess.ExtractAsync(mode,root,progress,CancellationToken.None);throw new Exception("Expected worker failure.");}
        catch(Exception error) when(error is IOException or InvalidDataException){if(mode=="fail")Check(error.Message.Contains("Fixture extraction failed"),"Child diagnostics must reach the caller.");}
        Check(File.ReadAllText(Path.Combine(root,"background.png"))=="worker result","Failed extraction must preserve the existing cache.");
        Check(!Directory.EnumerateDirectories(root,".worker-*").Any(),"Failure must remove only the worker staging directory.");
    }
    using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var cancellationProgress=new Capture(step=>{if(step.Percent==5)cancel.Cancel();});
    try{await AssetExtractionProcess.ExtractAsync("hang",root,cancellationProgress,cancel.Token);throw new Exception("Expected cancellation.");}
    catch(OperationCanceledException) { }
    Check(!Directory.EnumerateDirectories(root,".worker-*").Any(),"Cancellation must terminate/reap the worker and remove staging files.");
    Check(File.ReadAllText(Path.Combine(root,"background.png"))=="worker result","Cancellation must preserve the existing cache.");
    Console.WriteLine("PASS: real worker process, quoted paths, progress, nonzero/incomplete output, cancellation, staging cleanup and preserved cache.");
    return 0;
}
finally{Directory.Delete(root,true);}
static void Check(bool value,string message){if(!value)throw new Exception(message);}
sealed class Capture(Action<GameAssetExtractor.Step> callback):IProgress<GameAssetExtractor.Step>
{
    public void Report(GameAssetExtractor.Step step)=>callback(step);
}
namespace BloodborneLauncher.Core
{
    // Model the extraction contract; real texture/font conversion is covered separately.
    public static class GameAssetExtractor
    {
        public readonly record struct Step(int Percent,string Message);
        public static readonly string[] RequiredFiles={"background.png","font.ttf"};
        public static bool IsReady(string directory)=>RequiredFiles.Append("assets-manifest.json").All(name=>File.Exists(Path.Combine(directory,name)));
    }
}
