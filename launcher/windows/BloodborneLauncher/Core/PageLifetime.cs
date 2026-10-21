namespace BloodborneLauncher.Core;

/// <summary>Idempotent ownership of event subscriptions, timers and nested pages.</summary>
public sealed class PageLifetime : IDisposable
{
    private List<Action>? _releases=new();
    public void Add(Action release)
    {
        if(_releases==null)release();else _releases.Add(release);
    }
    public void Dispose()
    {
        var releases=_releases;
        if(releases==null)return;
        _releases=null;
        List<Exception>? failures=null;
        foreach(var release in releases)
            try { release(); }
            catch(Exception error) { (failures??=new()).Add(error); }
        if(failures!=null)throw new AggregateException("Page resource cleanup failed.",failures);
    }
}
