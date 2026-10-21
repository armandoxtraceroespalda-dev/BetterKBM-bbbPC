namespace BloodborneLauncher.Core;

/// <summary>Keep recently used settings pages, releasing evicted pages and their subscriptions.</summary>
public sealed class PageCache<TKey,TValue> : IDisposable where TKey:notnull
{
    private readonly int _capacity;
    private readonly Action<TValue> _release;
    private readonly Dictionary<TKey,(TValue Value,LinkedListNode<TKey> Node)> _items=new();
    private readonly LinkedList<TKey> _recent=new();
    public int Count=>_items.Count;
    public IEnumerable<TValue> Values=>_items.Values.Select(item=>item.Value);
    public bool TryGetValue(TKey key,out TValue value)
    {
        if(_items.TryGetValue(key,out var item)){value=item.Value;return true;}
        value=default!;return false;
    }
    public PageCache(int capacity,Action<TValue> release)
    {
        if(capacity<1)throw new ArgumentOutOfRangeException(BloodborneLauncher.Core.UiText.T(nameof(capacity)));
        _capacity=capacity;_release=release;
    }
    public TValue GetOrCreate(TKey key,Func<TValue> create)
    {
        if(_items.TryGetValue(key,out var found))
        { _recent.Remove(found.Node);_recent.AddLast(found.Node);return found.Value; }
        // Failed construction must not evict a working page.
        var value=create();
        _items.Add(key,(value,_recent.AddLast(key)));
        if(_items.Count>_capacity)
        {
            var oldest=_recent.First!;var removed=_items[oldest.Value].Value;
            _recent.RemoveFirst();_items.Remove(oldest.Value);_release(removed);
        }
        return value;
    }
    public void Clear()
    {
        var values=_items.Values.Select(item=>item.Value).ToArray();_items.Clear();_recent.Clear();
        List<Exception>? failures=null;
        foreach(var value in values)
            try { _release(value); }
            catch(Exception error) { (failures??=new()).Add(error); }
        if(failures!=null)throw new AggregateException("Page cache cleanup failed.",failures);
    }
    public void Dispose()=>Clear();
}
