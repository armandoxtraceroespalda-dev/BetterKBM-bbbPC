using System.Windows;
using System.Windows.Media;
using BloodborneLauncher.Core;
namespace BloodborneLauncher.Views;

/// <summary>Caption-sized oval shade and stable native glow; no rectangular bar or underline.</summary>
public sealed class MenuTabSurface : FrameworkElement
{
    public static readonly DependencyProperty HighlightProperty=DependencyProperty.Register(nameof(Highlight),typeof(bool),typeof(MenuTabSurface),new FrameworkPropertyMetadata(false,FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty CaptionWidthProperty=DependencyProperty.Register(nameof(CaptionWidth),typeof(double),typeof(MenuTabSurface),new FrameworkPropertyMetadata(100.0,FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty CaptionHeightProperty=DependencyProperty.Register(nameof(CaptionHeight),typeof(double),typeof(MenuTabSurface),new FrameworkPropertyMetadata(32.0,FrameworkPropertyMetadataOptions.AffectsRender));
    public bool Highlight {get=>(bool)GetValue(HighlightProperty);set=>SetValue(HighlightProperty,value);}
    public double CaptionWidth {get=>(double)GetValue(CaptionWidthProperty);set=>SetValue(CaptionWidthProperty,value);}
    public double CaptionHeight {get=>(double)GetValue(CaptionHeightProperty);set=>SetValue(CaptionHeightProperty,value);}
    private static readonly Brush Shade=CreateShade();
    private static Brush CreateShade()
    {
        var brush=new RadialGradientBrush { Center=new Point(0.5,0.5),GradientOrigin=new Point(0.5,0.5),RadiusX=0.5,RadiusY=0.5 };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(205,0,0,0),0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(185,0,0,0),0.55));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0,0,0,0),1));brush.Freeze();return brush;
    }
    protected override void OnRender(DrawingContext dc)
    {
        double w=ActualWidth,h=ActualHeight;if(w<1 || h<1)return;
        dc.DrawEllipse(Shade,null,new Point(w/2,h/2),Math.Min(w*0.64,CaptionWidth/2+36),Math.Min(h/2,CaptionHeight*0.8+8));
        if(Highlight && TryFindResource("MenuGlowBrush") is Brush glow)
        {
            var band=TabMetrics.Glow(w,h,CaptionWidth,CaptionHeight);
            dc.DrawRectangle(glow,null,new Rect(band.X,band.Y,band.Width,band.Height));
        }
    }
}
