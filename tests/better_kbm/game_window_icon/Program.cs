using BloodborneLauncher.Core;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
var parents = new Dictionary<uint, uint> { [100] = 9, [101] = 100, [102] = 101, [103] = 9, [104] = 0 };
Check(GameWindowIcon.IsDescendant(100, 100, parents), "A direct custom game process belongs to its own session.");
Check(GameWindowIcon.IsDescendant(102, 100, parents), "Python wrappers may create the actual game through more than one child.");
Check(!GameWindowIcon.IsDescendant(103, 100, parents), "Another game with a shared parent must never receive this session's icon.");
Check(!GameWindowIcon.IsDescendant(104, 100, parents), "An unrelated root process must be excluded.");
Check(!GameWindowIcon.IsDescendant(999, 100, parents), "Unknown or exited processes must be excluded.");
Check(!GameWindowIcon.IsDescendant(12, 100, new Dictionary<uint, uint> { [12] = 13, [13] = 12 }), "Malformed process cycles must terminate.");
Console.WriteLine("PASS: game-window icon targeting is restricted to the launched process tree; missing processes and cycles are safe.");

namespace BloodborneLauncher.Core
{
    internal enum LogLevel { Warn }
    internal static class LauncherDiagnostics { public static void Record(string message, LogLevel level) { } }
}
