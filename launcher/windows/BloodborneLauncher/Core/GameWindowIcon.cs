using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BloodborneLauncher.Core;

/// <summary>Apply locally extracted artwork to this session's game windows without changing the engine.</summary>
internal static class GameWindowIcon
{
    public static Task WatchAsync(Process session, string path, CancellationToken cancel) => Task.Run(async () =>
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(path)) return;
        nint small = 0, large = 0;
        try
        {
            small = LoadImage(0, path, 1, 16, 16, 0x10);
            large = LoadImage(0, path, 1, 48, 48, 0x10);
            if (small == 0 || large == 0) throw new IOException(BloodborneLauncher.Core.UiText.T("Could not load the local game icon."));
            var applied = new HashSet<nint>();
            uint root = (uint)session.Id;
            while (!cancel.IsCancellationRequested && !session.HasExited)
            {
                var parents = ParentProcesses();
                EnumWindows((window, state) =>
                {
                    if (applied.Contains(window) || !IsWindowVisible(window) || GetWindow(window, 4) != 0) return true;
                    GetWindowThreadProcessId(window, out uint pid);
                    if (!IsDescendant(pid, root, parents)) return true;
                    var kind = new StringBuilder(256);
                    GetClassName(window, kind, kind.Capacity);
                    if (kind.ToString() is "ConsoleWindowClass" or "PseudoConsoleWindow") return true;
                    // A hung window must not block the game or launcher. Handles live for the session.
                    bool setSmall = SendMessageTimeout(window, 0x80, 0, small, 2, 200, out _) != 0;
                    bool setLarge = SendMessageTimeout(window, 0x80, 1, large, 2, 200, out _) != 0;
                    if (setSmall && setLarge) applied.Add(window);
                    return true;
                }, 0);
                await Task.Delay(1000, cancel).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
        catch (Exception error) when (error is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            LauncherDiagnostics.Record("Game window icon: " + error.Message, LogLevel.Warn);
        }
        finally
        {
            if (small != 0) DestroyIcon(small);
            if (large != 0) DestroyIcon(large);
        }
    });

    internal static bool IsDescendant(uint pid, uint root, IReadOnlyDictionary<uint, uint> parents)
    {
        // A corrupt/cyclic snapshot cannot keep the watcher busy indefinitely.
        for (int depth = 0; depth <= parents.Count; depth++)
        {
            if (pid == root) return true;
            if (pid == 0 || !parents.TryGetValue(pid, out pid)) return false;
        }
        return false;
    }

    private static Dictionary<uint, uint> ParentProcesses()
    {
        var result = new Dictionary<uint, uint>();
        nint snapshot = CreateToolhelp32Snapshot(2, 0);
        if (snapshot == -1) return result;
        try
        {
            var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>(), FileName = "" };
            if (Process32First(snapshot, ref entry))
                do { result[entry.ProcessId] = entry.ParentId; } while (Process32Next(snapshot, ref entry));
        }
        finally { CloseHandle(snapshot); }
        return result;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry
    {
        public uint Size, Usage, ProcessId;
        public nuint DefaultHeap;
        public uint ModuleId, Threads, ParentId;
        public int Priority;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string FileName;
    }
    private delegate bool WindowCallback(nint window, nint state);
    [DllImport("kernel32.dll")] private static extern nint CreateToolhelp32Snapshot(uint flags, uint process);
    [DllImport("kernel32.dll", EntryPoint = "Process32FirstW", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool Process32First(nint snapshot, ref ProcessEntry entry);
    [DllImport("kernel32.dll", EntryPoint = "Process32NextW", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool Process32Next(nint snapshot, ref ProcessEntry entry);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseHandle(nint handle);
    [DllImport("user32.dll", EntryPoint = "LoadImageW", CharSet = CharSet.Unicode)] private static extern nint LoadImage(nint instance, string path, uint type, int width, int height, uint flags);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyIcon(nint icon);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool EnumWindows(WindowCallback callback, nint state);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint window, StringBuilder name, int size);
    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW")] private static extern nint SendMessageTimeout(nint window, uint message, nuint param, nint value, uint flags, uint timeout, out nuint result);
}
