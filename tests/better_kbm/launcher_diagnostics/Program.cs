using System.Globalization;
using BloodborneLauncher.Core;

CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
var directory=Path.Combine(Path.GetTempPath(),"bb-launcher-diagnostics-"+Guid.NewGuid().ToString("N"));
Paths.ConfigDir=directory;
try
{
    LauncherDiagnostics.Start();
    string log;
    if(!LauncherDiagnostics.DetailedEnabled)
    {
        // Warm the no-op singleton, then require an allocation-free disabled measurement path.
        LauncherDiagnostics.Measure("No trace").Dispose();
        long before=GC.GetAllocatedBytesForCurrentThread();
        for(int index=0;index<10000;index++)LauncherDiagnostics.Measure("No trace").Dispose();
        Check(GC.GetAllocatedBytesForCurrentThread()==before,"Disabled tracing must not allocate or sample process memory.");
        log=File.ReadAllText(LauncherDiagnostics.LogPath);
        Check(!log.Contains("Begin No trace"),"Default navigation must not emit measurement intervals.");
    }
    else
    {
    var phase=LauncherDiagnostics.Measure("Allocation fixture");
    var payload=new byte[2*1024*1024];payload[0]=1;
    phase.Dispose();phase.Dispose();GC.KeepAlive(payload);
    try
    {
        using(LauncherDiagnostics.Measure("Failure fixture"))
            throw new InvalidOperationException("Expected test failure");
    }
    catch(InvalidOperationException) { }
    log=File.ReadAllText(LauncherDiagnostics.LogPath);
    Check(log.Contains("Bloodborne UI 32"),"The build marker must identify this diagnostic revision.");
    Check(log.Split("End Allocation fixture").Length==2,"Repeated Dispose must not duplicate an interval.");
    Check(log.Contains("interval allocated=2."),"Allocation intervals must observe a real multi-MiB allocation.");
    Check(log.Contains("Begin Failure fixture") && log.Contains("End Failure fixture"),"Failed operations still close their interval.");
    foreach(var field in new[]{"working set=","managed=","last GC heap=","last GC fragmentation=","allocated total=","collections="})
        Check(log.Contains(field),"Missing evidence: "+field);
    }
    LauncherDiagnostics.Start();
    Check(File.ReadAllText(LauncherDiagnostics.PreviousLogPath)==log,"The previous run must survive restarting for a warm-cache comparison.");
    Check(!File.ReadAllText(LauncherDiagnostics.LogPath).Contains("Allocation fixture"),"Each run must start a separate evidence file.");
    File.WriteAllText(LauncherDiagnostics.LogPath,new string('x',262145));
    LauncherDiagnostics.Record("After rotation",LogLevel.Info);
    Check(File.ReadAllText(LauncherDiagnostics.LogPath).StartsWith("Log rotated."),"The diagnostic must stay bounded.");
    Directory.Delete(directory,true);File.WriteAllText(directory,"File blocking the log directory");
    LauncherDiagnostics.Record("Unwritable fixture",LogLevel.Info);
    Console.WriteLine("PASS: real allocation intervals, exception cleanup, idempotent disposal, memory fields, bounded log and nonfatal I/O failure.");
}
finally
{
    if(Directory.Exists(directory))Directory.Delete(directory,true);
    if(File.Exists(directory))File.Delete(directory);
}
static void Check(bool condition,string message){if(!condition)throw new Exception(message);}

namespace BloodborneLauncher.Core
{
    public static class Paths { public static string ConfigDir { get; set; }=""; }
    public enum LogLevel { Info,Step,Good,Warn,Bad }
}
