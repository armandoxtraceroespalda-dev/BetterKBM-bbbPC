using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace BloodborneLauncher.Controls;

public enum ControlAppearance { Primary, Secondary, Transparent, Danger, Caution, Success }
public sealed class Button : System.Windows.Controls.Button
{
    public static readonly DependencyProperty IconProperty=DependencyProperty.Register(nameof(Icon),typeof(object),typeof(Button));
    public object? Icon {get=>GetValue(IconProperty);set=>SetValue(IconProperty,value);}
    public ControlAppearance Appearance {get;set;}
}
public sealed class Card : ContentControl { }
public sealed class TextBox : System.Windows.Controls.TextBox
{
    public static readonly DependencyProperty PlaceholderTextProperty=DependencyProperty.Register(nameof(PlaceholderText),typeof(string),typeof(TextBox),new PropertyMetadata(""));
    public string PlaceholderText {get=>(string)GetValue(PlaceholderTextProperty);set=>SetValue(PlaceholderTextProperty,value);}
    public bool PlaceholderEnabled {get;set;}=true;
    public object? Icon {get;set;}
}
public sealed class ToggleSwitch : ToggleButton { }
public enum InfoBarSeverity { Informational, Success, Warning, Error }
public sealed class InfoBar : Control
{
    public static readonly DependencyProperty TitleProperty=DependencyProperty.Register(nameof(Title),typeof(string),typeof(InfoBar),new PropertyMetadata(""));
    public static readonly DependencyProperty MessageProperty=DependencyProperty.Register(nameof(Message),typeof(string),typeof(InfoBar),new PropertyMetadata(""));
    public string Title {get=>(string)GetValue(TitleProperty);set=>SetValue(TitleProperty,value);}
    public string Message {get=>(string)GetValue(MessageProperty);set=>SetValue(MessageProperty,value);}
    public bool IsClosable {get;set;}
    public static readonly DependencyProperty SeverityProperty=DependencyProperty.Register(nameof(Severity),typeof(InfoBarSeverity),typeof(InfoBar),new PropertyMetadata(InfoBarSeverity.Informational));
    public InfoBarSeverity Severity {get=>(InfoBarSeverity)GetValue(SeverityProperty);set=>SetValue(SeverityProperty,value);}
    public bool IsOpen {get=>Visibility==Visibility.Visible;set=>Visibility=value?Visibility.Visible:Visibility.Collapsed;}
}
/// <summary>Use Windows' small system-symbol font instead of loading a separate icon-font library.</summary>
public sealed class SymbolIcon : FrameworkElement
{
    public static readonly DependencyProperty ForegroundProperty=Control.ForegroundProperty.AddOwner(typeof(SymbolIcon),new FrameworkPropertyMetadata(Brushes.Bisque,FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty FontSizeProperty=Control.FontSizeProperty.AddOwner(typeof(SymbolIcon),new FrameworkPropertyMetadata(18d,FrameworkPropertyMetadataOptions.AffectsMeasure|FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SymbolProperty=DependencyProperty.Register(nameof(Symbol),typeof(SymbolRegular),typeof(SymbolIcon),new FrameworkPropertyMetadata(default(SymbolRegular),FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush Foreground {get=>(Brush)GetValue(ForegroundProperty);set=>SetValue(ForegroundProperty,value);}
    public double FontSize {get=>(double)GetValue(FontSizeProperty);set=>SetValue(FontSizeProperty,value);}
    public SymbolRegular Symbol {get=>(SymbolRegular)GetValue(SymbolProperty);set=>SetValue(SymbolProperty,value);}
    public bool Filled {get;set;}
    private static readonly Typeface Face=new(new FontFamily("Segoe MDL2 Assets"),FontStyles.Normal,FontWeights.Normal,FontStretches.Normal);
    protected override Size MeasureOverride(Size available)=>new(FontSize+2,FontSize+2);
    protected override void OnRender(DrawingContext dc)
    {
        string glyph=Symbol switch {
            SymbolRegular.Add24=>"\uE710",SymbolRegular.Delete24=>"\uE74D",SymbolRegular.Dismiss24=>"\uE711",
            SymbolRegular.Folder24 or SymbolRegular.FolderArrowRight24 or SymbolRegular.FolderLink24 or SymbolRegular.FolderOpen24=>"\uE8B7",
            SymbolRegular.Trophy24=>"\uE734",SymbolRegular.CheckmarkCircle24=>"\uE73E",SymbolRegular.Warning24 or SymbolRegular.Alert24 or SymbolRegular.ErrorCircle24=>"\uE7BA",
            SymbolRegular.Info16=>"\uE946",SymbolRegular.Save24=>"\uE74E",SymbolRegular.Copy24=>"\uE8C8",SymbolRegular.ClipboardPaste24=>"\uE77F",
            SymbolRegular.Open24 or SymbolRegular.ArrowExit20=>"\uE8A7",SymbolRegular.ArrowDownload24 or SymbolRegular.DocumentArrowDown20=>"\uE896",
            SymbolRegular.ArrowUndo24 or SymbolRegular.ArrowReset24=>"\uE7A7",SymbolRegular.ArrowClockwise24 or SymbolRegular.ArrowRotateClockwise24 or SymbolRegular.ArrowSync24=>"\uE72C",
            SymbolRegular.ArrowUp24 or SymbolRegular.ArrowExportUp24=>"\uE74A",SymbolRegular.ArrowDown24 or SymbolRegular.ArrowImport24=>"\uE74B",
            SymbolRegular.ArrowSwap24 or SymbolRegular.ArrowBidirectionalLeftRight24=>"\uE8AB",SymbolRegular.Search24=>"\uE721",
            SymbolRegular.Person24 or SymbolRegular.PersonAccounts24 or SymbolRegular.PersonAdd24 or SymbolRegular.PersonDelete24=>"\uE77B",
            SymbolRegular.People24 or SymbolRegular.PeopleCommunity24=>"\uE716",SymbolRegular.Wrench24 or SymbolRegular.DocumentSettings20=>"\uE713",
            SymbolRegular.Speaker124 or SymbolRegular.Speaker224=>"\uE767",SymbolRegular.MusicNote224=>"\uE8D6",
            SymbolRegular.Keyboard24 or SymbolRegular.KeyboardShift24=>"\uE765",SymbolRegular.XboxController24 or SymbolRegular.Games24=>"\uE7FC",
            SymbolRegular.Desktop24 or SymbolRegular.DeveloperBoard24=>"\uE7F4",SymbolRegular.FullScreenMaximize24 or SymbolRegular.ResizeLarge24 or SymbolRegular.ArrowMaximize24=>"\uE740",
            SymbolRegular.Image24=>"\uEB9F",SymbolRegular.Camera24=>"\uE722",SymbolRegular.Globe24 or SymbolRegular.Router24 or SymbolRegular.Server24=>"\uE774",
            SymbolRegular.History24 or SymbolRegular.Timer24 or SymbolRegular.HourglassHalf24=>"\uE823",SymbolRegular.Pin24=>"\uE718",
            SymbolRegular.TextFont24=>"\uE8D2",SymbolRegular.TextDescription24 or SymbolRegular.DocumentText24 or SymbolRegular.TextBulletListSquare24=>"\uE8A5",
            SymbolRegular.WindowConsole20 or SymbolRegular.Code24=>"\uE756",SymbolRegular.Link24=>"\uE71B",SymbolRegular.Share24=>"\uE72D",
            SymbolRegular.ZoomIn24=>"\uE8A3",SymbolRegular.ZoomOut24=>"\uE71F",SymbolRegular.BrightnessHigh24 or SymbolRegular.WeatherSunny24=>"\uE706",
            SymbolRegular.PlayCircle24=>"\uE768",SymbolRegular.FastForward24=>"\uE892",SymbolRegular.Shield24=>"\uE730",SymbolRegular.Password24=>"\uE72E",
            SymbolRegular.EyeOff24=>"\uE7B3",SymbolRegular.Rename24=>"\uE70F",SymbolRegular.SlideSize24 or SymbolRegular.RatioOneToOne24=>"\uE91B",
            _=>"\uE10C"
        };
        var text=new FormattedText(glyph,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,Face,FontSize,Foreground,VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(text,new Point((ActualWidth-text.Width)/2,(ActualHeight-text.Height)/2));
    }
}
