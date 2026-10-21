using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BloodborneLauncher.Core;
namespace BloodborneLauncher.Views;

public static class MenuSizing
{
    public static double SelectorWidth(string label,double maximum=240)
    {
        var text=new FormattedText(label,CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,
            new Typeface(LauncherAppearance.MenuFont,FontStyles.Normal,FontWeights.Normal,FontStretches.Normal),21,Brushes.White,1);
        return Math.Min(maximum,Math.Max(100,Math.Ceiling(text.WidthIncludingTrailingWhitespace+56)));
    }
}
