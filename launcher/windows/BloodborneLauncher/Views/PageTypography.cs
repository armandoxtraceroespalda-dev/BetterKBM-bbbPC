using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using BloodborneLauncher.Core;

namespace BloodborneLauncher.Views;

/// <summary>Scale explicit page fonts once per size change, without scanning on mouse or layout events.</summary>
public static class PageTypography
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(PageTypography), new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.Inherits, Changed));
    public static readonly DependencyProperty ScaleProperty = DependencyProperty.RegisterAttached(
        "Scale", typeof(double), typeof(PageTypography), new FrameworkPropertyMetadata(1d,
            FrameworkPropertyMetadataOptions.Inherits, Changed));
    private static readonly DependencyProperty BaseFontProperty = DependencyProperty.RegisterAttached(
        "BaseFont", typeof(double), typeof(PageTypography), new PropertyMetadata(double.NaN));
    private static readonly DependencyProperty BaseHeightProperty = DependencyProperty.RegisterAttached(
        "BaseHeight", typeof(double), typeof(PageTypography), new PropertyMetadata(double.NaN));
    private static readonly DependencyProperty LastFontProperty = DependencyProperty.RegisterAttached(
        "LastFont", typeof(double), typeof(PageTypography), new PropertyMetadata(double.NaN));
    private static readonly ScaleConverter Converter = new();
    private static bool _registered;
    public static bool GetEnabled(DependencyObject value) => (bool)value.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject value, bool enabled) => value.SetValue(EnabledProperty, enabled);
    public static double GetScale(DependencyObject value) => (double)value.GetValue(ScaleProperty);
    public static void SetScale(DependencyObject value, double scale) => value.SetValue(ScaleProperty, scale);

    public static void Attach(FrameworkElement page)
    {
        if (!_registered)
        {
            _registered = true;
            EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(Loaded), true);
        }
        SetEnabled(page, true);
        var binding = new MultiBinding { Converter = Converter };
        foreach (string dimension in new[] { "ActualWidth", "ActualHeight" })
            binding.Bindings.Add(new Binding(dimension) {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Window), 1) });
        BindingOperations.SetBinding(page, ScaleProperty, binding);
    }

    private static void Loaded(object sender, RoutedEventArgs args)
    {
        if (sender is ToolTip tooltip) { SetEnabled(tooltip, false); return; }
        if (sender is DependencyObject node && GetEnabled(node)) Apply(node);
    }
    private static void Changed(DependencyObject node, DependencyPropertyChangedEventArgs args) => Apply(node);
    private static void Apply(DependencyObject node)
    {
        var font = node is TextBlock ? TextBlock.FontSizeProperty : node is Control ? Control.FontSizeProperty : null;
        if (font == null) return;
        double original = (double)node.GetValue(BaseFontProperty);
        if (double.IsNaN(original))
        {
            if (!GetEnabled(node) || node is ToolTip) return;
            var source = DependencyPropertyHelper.GetValueSource(node, font);
            // Inherited/template-bound fonts already follow their scaled owner.
            if (source.IsExpression || source.BaseValueSource is BaseValueSource.Inherited or BaseValueSource.Default
                or BaseValueSource.ParentTemplate or BaseValueSource.ParentTemplateTrigger) return;
            original = (double)node.GetValue(font);
            node.SetValue(BaseFontProperty, original);
        }
        else if (Math.Abs((double)node.GetValue(font) - (double)node.GetValue(LastFontProperty)) > 0.001)
        {
            // A heading or binding cell may set its intended base size after joining the page.
            original = (double)node.GetValue(font);
            node.SetValue(BaseFontProperty, original);
        }
        double scale = GetEnabled(node) ? Math.Clamp(GetScale(node), 0.7, 1.3) : 1;
        node.SetCurrentValue(font, original * scale);
        node.SetValue(LastFontProperty, original * scale);
        if (node is Control control && (node is TextBoxBase or ComboBox) && !double.IsNaN(control.Height))
        {
            double height = (double)node.GetValue(BaseHeightProperty);
            if (double.IsNaN(height)) { height = control.Height; node.SetValue(BaseHeightProperty, height); }
            // Fixed-height editors need room for a larger line and their existing padding.
            control.SetCurrentValue(FrameworkElement.HeightProperty,
                Math.Max(height, control.FontSize * 1.5 + control.Padding.Top + control.Padding.Bottom + 2));
        }
    }
    private sealed class ScaleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            values.Length == 2 && values[0] is double width && values[1] is double height
                ? WindowTextScale.For(width, height) : 1d;
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
