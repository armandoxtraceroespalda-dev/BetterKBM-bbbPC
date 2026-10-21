using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace BloodborneLauncher.Views;

/// <summary>Each viewport owns its clocks; interrupted transitions release clocks and callbacks immediately.</summary>
public static class MenuTransition
{
    private static readonly DependencyProperty StateProperty=DependencyProperty.RegisterAttached(
        "State",typeof(Transition),typeof(MenuTransition),new PropertyMetadata(null));
    public static void Reset(FrameworkElement viewport)
    {
        var previous=viewport.GetValue(StateProperty) as Transition;
        viewport.ClearValue(StateProperty);previous?.Dispose();
        viewport.ApplyAnimationClock(UIElement.OpacityProperty,null);viewport.Opacity=1;
        if(viewport.RenderTransform is TranslateTransform offset)
        { offset.ApplyAnimationClock(TranslateTransform.YProperty,null);offset.Y=0; }
        else viewport.RenderTransform=new TranslateTransform();
    }
    public static async Task LeaveAsync(FrameworkElement viewport,CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        double opacity=viewport.Opacity;
        double y=(viewport.RenderTransform as TranslateTransform)?.Y??0;
        Reset(viewport);
        if(!SystemParameters.ClientAreaAnimation)return;
        viewport.Opacity=opacity;((TranslateTransform)viewport.RenderTransform).Y=y;
        var done=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var state=new Transition(viewport,()=>done.TrySetResult(),hold:true);
        viewport.SetValue(StateProperty,state);
        using var registration=cancel.Register(()=>done.TrySetCanceled(cancel));
        state.Start(new DoubleAnimation(viewport.Opacity,0,TimeSpan.FromMilliseconds(180)) {FillBehavior=FillBehavior.HoldEnd},false);
        try { await done.Task; }
        finally
        {
            if(cancel.IsCancellationRequested && ReferenceEquals(viewport.GetValue(StateProperty),state))Reset(viewport);
        }
    }
    public static void Enter(FrameworkElement viewport,bool rise,Action? completed=null)
    {
        Reset(viewport);
        if(!SystemParameters.ClientAreaAnimation) { completed?.Invoke();return; }
        var state=new Transition(viewport,completed,hold:false);viewport.SetValue(StateProperty,state);
        state.Start(new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(280)) {FillBehavior=FillBehavior.Stop},rise);
    }
    private sealed class Transition : IDisposable
    {
        private readonly FrameworkElement _viewport;
        private readonly TranslateTransform _offset;
        private readonly bool _hold;
        private Action? _completed;
        private AnimationClock? _fade,_rise;
        private bool _disposed;
        internal Transition(FrameworkElement viewport,Action? completed,bool hold)
        { _viewport=viewport;_offset=(TranslateTransform)viewport.RenderTransform;_completed=completed;_hold=hold; }
        internal void Start(DoubleAnimation fade,bool rise)
        {
            _fade=(AnimationClock)fade.CreateClock(true);_fade.Completed+=Completed;
            _viewport.ApplyAnimationClock(UIElement.OpacityProperty,_fade,HandoffBehavior.SnapshotAndReplace);
            if(rise)
            {
                var animation=new DoubleAnimation(20,0,TimeSpan.FromMilliseconds(280)) {
                    FillBehavior=FillBehavior.Stop,EasingFunction=new CubicEase {EasingMode=EasingMode.EaseOut}
                };
                _rise=(AnimationClock)animation.CreateClock(true);
                _offset.ApplyAnimationClock(TranslateTransform.YProperty,_rise,HandoffBehavior.SnapshotAndReplace);
            }
        }
        private void Completed(object? sender,EventArgs args)
        {
            if(_disposed || !ReferenceEquals(_viewport.GetValue(StateProperty),this))return;
            var callback=_completed;_completed=null;
            if(_fade!=null)_fade.Completed-=Completed;
            // A completed exit keeps only its final opacity until the new page is installed.
            if(!_hold)Reset(_viewport);
            callback?.Invoke();
        }
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;_completed=null;
            if(_fade!=null) { _fade.Completed-=Completed;_fade.Controller?.Remove();_fade=null; }
            _rise?.Controller?.Remove();_rise=null;
        }
    }
}
