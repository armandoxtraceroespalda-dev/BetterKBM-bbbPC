using System.Diagnostics;
using System.IO;

namespace BloodborneLauncher.Core;

/// <summary>Keep ATRAC9 decoder tables and bank parsing in a worker that exits after extraction.</summary>
internal static class MenuSoundExtractionProcess
{
    public const string WorkerArgument = "--extract-launcher-sounds";
    public static async Task ExtractAsync(string game, string gui, IProgress<GameAssetExtractor.Step> progress, CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        if(GameMenuSoundExtractor.IsReady(gui)) { progress.Report(new(100,"Menu sounds ready")); return; }
        Directory.CreateDirectory(gui);
        string staging = Path.Combine(gui, ".sound-worker-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(GameMenuSoundExtractor.SoundDirectory(staging));
        try
        {
            foreach (var name in GameMenuSoundExtractor.Samples.Keys)
            {
                var existing = Path.Combine(GameMenuSoundExtractor.SoundDirectory(gui), name);
                if (GameMenuSoundExtractor.ValidClip(existing))
                    File.Copy(existing, Path.Combine(GameMenuSoundExtractor.SoundDirectory(staging), name));
            }
            var command = new ProcessStartInfo(Environment.ProcessPath ?? throw new InvalidOperationException(BloodborneLauncher.Core.UiText.T("The launcher executable path is unavailable.")));
            if (Path.GetFileNameWithoutExtension(command.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                command.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
            command.ArgumentList.Add(WorkerArgument); command.ArgumentList.Add(game); command.ArgumentList.Add(staging);
            await AssetExtractionProcess.RunCommandAsync(command, progress, cancel);
            cancel.ThrowIfCancellationRequested();
            if (!GameMenuSoundExtractor.IsReady(staging)) throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("The sound worker did not produce valid menu clips."));
            var target = GameMenuSoundExtractor.SoundDirectory(gui); Directory.CreateDirectory(target);
            foreach (var name in GameMenuSoundExtractor.Samples.Keys)
                if (!GameMenuSoundExtractor.ValidClip(Path.Combine(target, name)))
                    File.Move(Path.Combine(GameMenuSoundExtractor.SoundDirectory(staging), name), Path.Combine(target, name), overwrite: true);
            var manifest = Path.Combine(GameMenuSoundExtractor.SoundDirectory(staging), "assets-manifest.json");
            if (File.Exists(manifest)) File.Move(manifest, Path.Combine(target, "assets-manifest.json"), overwrite: true);
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true); }
    }
}
