namespace BloodborneLauncher.Core;

/// <summary>Phrase recognition for the easter egg; the launcher persists access without enabling any cheat.</summary>
internal sealed class CheatAccess
{
    internal static CheatAccess Session { get; } = new();
    private const string UnlockPhrase = "gitgud", LockPhrase = "igotgud";
    private string _prefix = "";
    internal bool Unlocked { get; private set; }
    internal void Restore(bool unlocked)
    {
        Unlocked |= unlocked;
        if (Unlocked) _prefix = "";
    }
    internal void ResetInput() => _prefix = "";
    internal bool Feed(string text, bool allowed = true)
    {
        if (!allowed) { ResetInput(); return false; }
        string phrase = Unlocked ? LockPhrase : UnlockPhrase;
        foreach (char value in text)
        {
            _prefix += char.ToLowerInvariant(value);
            while (_prefix.Length > 0 && !phrase.StartsWith(_prefix, StringComparison.Ordinal)) _prefix = _prefix[1..];
            if (_prefix != phrase) continue;
            Unlocked = !Unlocked; _prefix = ""; return true;
        }
        return false;
    }
}
