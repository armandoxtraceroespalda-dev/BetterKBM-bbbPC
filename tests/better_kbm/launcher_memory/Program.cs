using BloodborneLauncher.Core;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
var source=new Publisher();var cache=new PageCache<int,TestPage>(3,p=>p.Lifetime.Dispose());
var storedSettings=new Dictionary<int,int>();
var first=cache.GetOrCreate(1,()=>new TestPage(source));
cache.GetOrCreate(2,()=>new TestPage(source));
Check(ReferenceEquals(cache.GetOrCreate(1,()=>throw new Exception()),first),"Recent pages must be reused.");
try{cache.GetOrCreate(10,()=>throw new InvalidOperationException());}catch(InvalidOperationException){}
Check(cache.Count==2 && source.Listeners==2,"Failed page construction must preserve live pages.");
cache.GetOrCreate(3,()=>new TestPage(source));cache.GetOrCreate(1,()=>throw new Exception());
cache.GetOrCreate(4,()=>new TestPage(source));
Check(cache.TryGetValue(1,out _) && !cache.TryGetValue(2,out _),"Eviction must remove the least recently used page.");
source.Raise();Check(first.Calls==1,"Cached pages retain live callbacks.");
for(int i=5;i<205;i++)
{
    storedSettings[i]=i*2;
    var page=cache.GetOrCreate(i,()=>new TestPage(source));
    source.Raise();
    Check(source.Listeners==3 && cache.Count==3,"Navigation cannot accumulate page event handlers.");
}
int retiredCalls=first.Calls;source.Raise();Check(first.Calls==retiredCalls,"Evicted pages must stop receiving events.");
Check(storedSettings[5]==10,"Eviction must preserve settings stored outside the UI.");
cache.Clear();cache.Clear();Check(source.Listeners==0,"Reload/clear must release all listeners exactly once.");
int cleanups=0;var lifetime=new PageLifetime();lifetime.Add(()=>cleanups++);lifetime.Dispose();lifetime.Dispose();
lifetime.Add(()=>cleanups++);Check(cleanups==2,"Late registration and repeated disposal must not retain cleanup closures.");
// Production uses a one-page budget: visiting every tab cannot retain dormant settings trees.
using(var active=new PageCache<int,TestPage>(1,p=>p.Lifetime.Dispose()))
{
    for(int i=0;i<200;i++)
    {
        active.GetOrCreate(i,()=>new TestPage(source));
        Check(active.Count==1 && source.Listeners==1,"Active-only navigation retains one listener/page.");
    }
}
Check(source.Listeners==0,"Active-only cache disposal releases its final page.");
var window=TabMetrics.Glow(206,65,140,32);var maximized=TabMetrics.Glow(206,140,140,32);
Check(window.Width==maximized.Width && window.Height==maximized.Height,"Maximize must not make the same caption's glow rounder.");
var bold=TabMetrics.Glow(206,150,130,58);
Check(bold.Height>window.Height,"Larger Start type receives a suitably sized glow.");
foreach(var size in new[]{(206.0,65.0,140.0,32.0),(206.0,150.0,230.0,58.0),(80.0,44.0,130.0,32.0)})
{
    var band=TabMetrics.Glow(size.Item1,size.Item2,size.Item3,size.Item4);
    Check(band.X>=0 && band.X+band.Width<=size.Item1,"The full glow must fit the sidebar row at every window size.");
    Check(band.X>=4 && band.X+band.Width<=size.Item1-4,"The complete light must leave a small inset within the full sidebar row.");
}
Check(TabMetrics.Glow(244,80,140,32).Width>TabMetrics.Glow(206,65,140,32).Width,"Using the complete sidebar must recover the glow's soft wings.");
double baseline=TabMetrics.SidebarScale(1280,800),large=TabMetrics.SidebarScale(1920,1080);
Check(baseline==1 && large>baseline,"Larger windows must scale the sidebar column and typography together.");
Check(TabMetrics.SidebarScale(900,620)==0.85 && TabMetrics.SidebarScale(7680,4320)==1.6,"Extreme window sizes retain readable bounded typography.");
if(args.Length>0)
{
    using var file=File.OpenRead(args[0]);using var pe=new PEReader(file);var metadata=pe.GetMetadataReader();
    Check(!metadata.AssemblyReferences.Select(h=>metadata.GetString(metadata.GetAssemblyReference(h).Name)).Contains("Wpf.Ui"),"The shipped assembly must not retain WPF-UI.");
    var resources=metadata.ManifestResources.Select(h=>metadata.GetString(metadata.GetManifestResource(h).Name)).ToHashSet();
    foreach(var locale in new[]{"en","es","ru","ja","fr","de","it","pt-BR"})
        Check(resources.Contains("BloodborneLauncher.Localization."+locale+".json"),"Published launcher is missing its catalog: "+locale);
    Check(resources.Contains("BloodborneLauncher.Fonts.LauncherFallback.otf"),"Published launcher is missing the redistributable fallback font.");
    RuntimeGcAudit.Validate(pe,metadata);
    foreach(var handle in metadata.TypeDefinitions)
    {
        var type=metadata.GetTypeDefinition(handle);string name=metadata.GetString(type.Name);
        Check(name is not ("InteractionHeapReclaimer" or "IdleHeapReclaimer" or "IdleMemoryReclaimer"),
            "Superseded forced-collection experiments must not be compiled into the launcher.");
        var methods=type.GetMethods().Select(m=>metadata.GetString(metadata.GetMethodDefinition(m).Name)).ToArray();
        var fields=type.GetFields().Select(f=>metadata.GetString(metadata.GetFieldDefinition(f).Name)).ToArray();
        if(name=="LauncherPage")
        {
            var parent=metadata.GetTypeReference((TypeReferenceHandle)type.BaseType);
            Check(metadata.GetString(parent.Name)=="UserControl","Settings views must not use navigation Page instances.");
        }
        if(name=="MainWindow")Check(!methods.Any(m=>m is "InstallUpdateAsync" or "InstallVersionAsync" or "InstallAsync"),"Self-replacement entry points must not be compiled.");
        if(name=="Workspace")Check(!methods.Contains("CheckForUpdatesAsync"),"Background release queries must not be compiled.");
        if(name=="SettingsPage")Check(!fields.Any(f=>f is "_versions" or "_updateNow" or "_releases"),"Update controls and release caches must not be retained invisibly.");
    }
}
var retired=RetentionFixture.Create();
GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
Check(!retired.TryGetTarget(out _),"Diagnostic leases must not root retired views.");
Console.WriteLine("PASS: LRU and active-only navigation budgets; cleanup, settings, failed construction; stable glow aspect on maximize and larger Start captions.");


sealed class Publisher
{
    private Action? _changed;
    public event Action Changed {add{_changed+=value;Listeners++;}remove{_changed-=value;Listeners--;}}
    public int Listeners{get;private set;}
    public void Raise()=>_changed?.Invoke();
}
sealed class TestPage
{
    public PageLifetime Lifetime{get;}=new();
    public int Calls{get;private set;}
    public TestPage(Publisher source){source.Changed+=Changed;Lifetime.Add(()=>source.Changed-=Changed);}
    private void Changed()=>Calls++;
}

static class RetentionFixture
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static WeakReference<object> Create()
    {
        var view=new object();var lease=ViewRetention.Register(view);lease.Retire();return new(view);
    }
}
