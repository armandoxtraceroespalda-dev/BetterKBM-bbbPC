// Only WPF input enums and application paths are substituted; production profile/patch logic is linked directly.
namespace System.Windows.Input {
 public enum Key { A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, D0, D1, D2, D3, D4, D5, D6, D7, D8, D9, NumPad0, NumPad1, NumPad2, NumPad3, NumPad4, NumPad5, NumPad6, NumPad7, NumPad8, NumPad9, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, F16, F17, F18, F19, F20, F21, F22, F23, F24, Add, Apps, Back, CapsLock, Decimal, Delete, Divide, Down, End, Escape, Home, Insert, LWin, Left, LeftAlt, LeftCtrl, LeftShift, Multiply, NumLock, OemBackslash, OemCloseBrackets, OemComma, OemMinus, OemOpenBrackets, OemPeriod, OemPipe, OemPlus, OemQuestion, OemQuotes, OemSemicolon, OemTilde, PageDown, PageUp, Pause, PrintScreen, RWin, Return, Right, RightAlt, RightCtrl, RightShift, Scroll, Space, Subtract, Tab, Up }
 public enum MouseButton { Left, Right, Middle, XButton1, XButton2 }
}
namespace BloodborneLauncher.Core {
 public static class Paths {
  public static readonly string Root=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"betterkbm-profile-"+System.Guid.NewGuid());
  public static string ConfigDir=>Root; public static string DataDir=>Root; public static string EngineDir=>System.IO.Path.Combine(Root,"engine"); public static string IniPath=>System.IO.Path.Combine(Root,"lantern.ini");
 }
 public sealed class LauncherSettings { private readonly System.Collections.Generic.Dictionary<string,string> _values=new(); public void Set(string key,string value)=>_values[key]=value; public string Str(string key)=>_values.GetValueOrDefault(key,""); }
 public sealed record Effect(string Key,bool Default);
 public static class Choices { public static Effect[] Effects=>System.Array.Empty<Effect>(); }
 public static class GameCheck { public static string[] SupportedTitles=>new[]{"CUSA03173"}; }
}
