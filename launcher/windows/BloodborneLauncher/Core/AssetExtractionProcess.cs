using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace BloodborneLauncher.Core;

/// <summary>Cold extraction buffers belong to a short-lived worker, never the visible launcher heap.</summary>
public static class AssetExtractionProcess
{
    public const string WorkerArgument="--extract-launcher-assets";

    public static async Task ExtractAsync(string game,string output,IProgress<GameAssetExtractor.Step> progress,CancellationToken cancel)
    {
        Directory.CreateDirectory(output);
        var staging=Path.Combine(output,".worker-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            var command=new ProcessStartInfo(Environment.ProcessPath??throw new InvalidOperationException(BloodborneLauncher.Core.UiText.T("The launcher executable path is unavailable.")));
            // Framework-dependent development builds use dotnet; published builds run their own apphost.
            if(Path.GetFileNameWithoutExtension(command.FileName).Equals("dotnet",StringComparison.OrdinalIgnoreCase))
                command.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
            command.ArgumentList.Add(WorkerArgument);command.ArgumentList.Add(game);command.ArgumentList.Add(staging);
            await RunCommandAsync(command,progress,cancel);
            cancel.ThrowIfCancellationRequested();
            if(!GameAssetExtractor.IsReady(staging))throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("The asset worker did not produce a valid menu cache."));
            foreach(var name in GameAssetExtractor.RequiredFiles.Append("assets-manifest.json"))
                File.Move(Path.Combine(staging,name),Path.Combine(output,name),overwrite:true);
        }
        finally
        {
            // A cancelled worker may leave its own extraction directory; never delete the user's cache.
            if(Directory.Exists(staging))Directory.Delete(staging,recursive:true);
        }
    }

    internal static async Task RunCommandAsync(ProcessStartInfo command,IProgress<GameAssetExtractor.Step> progress,CancellationToken cancel)
    {
        command.UseShellExecute=false;command.CreateNoWindow=true;
        command.RedirectStandardOutput=true;command.RedirectStandardError=true;
        cancel.ThrowIfCancellationRequested();
        using var process=new Process { StartInfo=command };
        if(!process.Start())throw new IOException(BloodborneLauncher.Core.UiText.T("Could not start the local asset worker."));
        // Drain both pipes concurrently. Bound retained diagnostics even if a child fails noisily.
        var errors=ReadErrorsAsync(process.StandardError);
        try
        {
            while(await process.StandardOutput.ReadLineAsync(cancel) is { } line)
            {
                if(line.Length>4096)continue;
                try
                {
                    var step=JsonSerializer.Deserialize<GameAssetExtractor.Step>(line);
                    if(step.Message!=null && step.Percent is >=0 and <=100)progress.Report(step);
                }
                catch(JsonException) { }
            }
            await process.WaitForExitAsync(cancel);
            var detail=await errors;
            if(process.ExitCode!=0)throw new IOException(BloodborneLauncher.Core.UiText.Format("Local menu asset extraction failed: {0}",detail.Trim()));
        }
        finally
        {
            if(!process.HasExited)
            {
                try { process.Kill(entireProcessTree:true); }
                catch(InvalidOperationException) when(process.HasExited) { }
                await process.WaitForExitAsync();
            }
            await errors;
        }
    }

    private static async Task<string> ReadErrorsAsync(StreamReader reader)
    {
        var text=new System.Text.StringBuilder();var buffer=new char[1024];int count;
        while((count=await reader.ReadAsync(buffer))>0)
            if(text.Length<8192)text.Append(buffer,0,Math.Min(count,8192-text.Length));
        return text.ToString();
    }

    public sealed class ProgressWriter : IProgress<GameAssetExtractor.Step>
    {
        public void Report(GameAssetExtractor.Step step)=>Console.Out.WriteLine(JsonSerializer.Serialize(step));
    }
}
