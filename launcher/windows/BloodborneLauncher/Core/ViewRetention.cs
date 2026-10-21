namespace BloodborneLauncher.Core;

/// <summary>Weak diagnostics only: observing a retired view must never keep it alive.</summary>
public static class ViewRetention
{
    public sealed class Lease
    {
        internal readonly WeakReference<object> Target;
        internal bool Disposed;
        internal Lease(object view)=>Target=new(view);
        public void Retire()=>Disposed=true;
    }
    private static readonly List<Lease> Entries=new();
    private static long _created;
    public static Lease Register(object view)
    {
        Entries.RemoveAll(e=>!e.Target.TryGetTarget(out _));
        var lease=new Lease(view);Entries.Add(lease);_created++;return lease;
    }
    public static string DescribeAlive()
    {
        // Materialize type names only during an explicitly requested diagnostic sample.
        var types=new Dictionary<string,int>(StringComparer.Ordinal);
        foreach(var entry in Entries)
            if(entry.Target.TryGetTarget(out var view))
            {
                string name=view.GetType().Name+(entry.Disposed?" (retired)":" (active)");
                types[name]=types.GetValueOrDefault(name)+1;
            }
        return string.Join("; ",types.OrderBy(pair=>pair.Key).Select(pair=>pair.Key+"="+pair.Value));
    }
    public static (long Created,int Alive,int RetiredAlive) Snapshot()
    {
        Entries.RemoveAll(e=>!e.Target.TryGetTarget(out _));
        return (_created,Entries.Count,Entries.Count(e=>e.Disposed));
    }
}
