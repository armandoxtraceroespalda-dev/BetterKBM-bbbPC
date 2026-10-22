using System.Windows;
using System.Windows.Media;
namespace BloodborneLauncher.Views;

/// <summary>Layer the shared native highlight over a dark thumb without allocating a bitmap or blur effect.</summary>
public sealed class MenuSwitchGlow : FrameworkElement
{
    public static readonly DependencyProperty ActiveProperty = DependencyProperty.Register(
        nameof(Active), typeof(bool), typeof(MenuSwitchGlow),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public bool Active { get => (bool)GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }
    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        if (!Active || ActualWidth <= 0 || ActualHeight <= 0 || TryFindResource("MenuGlowBrush") is not Brush glow) return;
        double center = ActualWidth - 7 - 9;
        // Two passes strengthen the extracted highlight, including its light on the dark thumb.
        var halo = new Rect(center - 27, ActualHeight / 2 - 20, 54, 40);
        drawing.DrawRectangle(glow, null, halo);
        drawing.DrawRectangle(glow, null, halo);
    }
}
