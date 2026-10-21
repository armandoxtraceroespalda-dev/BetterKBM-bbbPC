namespace BloodborneLauncher.Core;

public enum MenuSoundCue { Hover, Select, Start, Change }

/// <summary>Short pointer bursts must not interrupt selection or the title-start sound.</summary>
internal sealed class MenuSoundGate
{
    private long _quietUntil, _lastHover = long.MinValue / 2, _lastSelection = long.MinValue / 2;
    public bool TryPlay(MenuSoundCue cue, long now, int durationMs = 0)
    {
        if (cue == MenuSoundCue.Hover)
        {
            if (now < _quietUntil || now - _lastHover < 70) return false;
            _lastHover = now;
            return true;
        }
        if (now - _lastSelection < 35 || now < _startUntil) return false;
        _lastSelection = now;
        _quietUntil = now + (durationMs > 0 ? durationMs + 25 : cue == MenuSoundCue.Start ? 1200 : 160);
        if (cue == MenuSoundCue.Start) _startUntil = _quietUntil;
        return true;
    }
    private long _startUntil;
}
