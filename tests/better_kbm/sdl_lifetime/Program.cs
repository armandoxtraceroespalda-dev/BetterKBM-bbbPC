using BloodborneLauncher.Core;
using System.Runtime.InteropServices;
Paths.BinariesDir=Path.GetFullPath(args[0]);
string log=Environment.GetEnvironmentVariable("SDL_LIFETIME_LOG")!;
File.Delete(log);
static void Check(bool value,string message){if(!value)throw new Exception(message);}
if(args.Contains("--broken"))
{
    Check(!SdlPad.Available,"A missing export must fail safely.");
    Check(File.ReadAllLines(log).SequenceEqual(new[]{"load","unload"}),"Partial-load failure must release the native module.");
    Console.WriteLine("PASS: partial native export failure releases its module reference.");return;
}
SdlPad.Stop();
Check(SdlPad.Available,"A library-only check must load the fixture.");
SdlPad.Stop();SdlPad.Stop();
Check(File.ReadAllLines(log).SequenceEqual(new[]{"load","quit","unload"}),"Even never-initialized libraries must unload exactly once.");
for(int i=0;i<25;i++)
{
    int before=File.ReadAllLines(log).Length;
    Check(SdlPad.Devices() is {Count:1},"Device enumeration must survive reloading.");
    Check(SdlPad.Held().SetEquals(new[]{"a","lefttrigger"}),"Fresh native delegates must read buttons and axes after reload.");
    SdlPad.Stop();SdlPad.Stop();
    var rows=File.ReadAllLines(log).Skip(before).ToArray();
    Check(rows.SequenceEqual(new[]{"load","init","open","close","quit-subsystem","quit","unload"}),"Every lease must close handles, quit SDL, and unload in order.");
}
Check(SdlPad.Devices() is {Count:1},"The polling fixture must open a real native pad.");
var native=NativeLibrary.Load(Path.Combine(Paths.BinariesDir,"SDL3.dll"));
try
{
 var button=Marshal.GetDelegateForFunctionPointer<SetButton>(NativeLibrary.GetExport(native,"SDL_TestSetButton"));
 var axis=Marshal.GetDelegateForFunctionPointer<SetAxis>(NativeLibrary.GetExport(native,"SDL_TestSetAxis"));
 ulong original=SdlPad.State();button(false);Check(SdlPad.State()!=original,"Physical button changes must remain visible.");
 button(true);axis(20000);Check(SdlPad.State()!=original,"Coarse stick changes must remain visible.");axis(0);
 Check(SdlPad.State()==original,"The original physical state must have the same fingerprint.");
 for(int index=0;index<1000;index++)SdlPad.State();
 long before=GC.GetAllocatedBytesForCurrentThread();
 for(int index=0;index<10000;index++)SdlPad.State();
 long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
 Check(allocated<=1024,"Warm native activity polling must not allocate per tick; observed bytes="+allocated);
 Console.WriteLine("PASS: 10,000 actual native state polls allocated "+allocated+" managed bytes; buttons and axes remain responsive.");
 SdlPad.Stop();
}
finally {NativeLibrary.Free(native);}
GC.Collect();GC.WaitForPendingFinalizers();
Console.WriteLine("PASS: 25 native unload/reload cycles preserve input, teardown order and idempotence.");
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void SetButton([MarshalAs(UnmanagedType.U1)] bool value);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void SetAxis(short value);
namespace BloodborneLauncher.Core
{
    public static class Paths {public static string BinariesDir="";}
    public static class GameEnvironment {public static IEnumerable<KeyValuePair<string,string>> SdlHints()=>Array.Empty<KeyValuePair<string,string>>();}
}
