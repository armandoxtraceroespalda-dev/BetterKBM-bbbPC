using System.Windows.Markup;
namespace BloodborneLauncher.Core;
public sealed class LocExtension : MarkupExtension
{
    public string Key{get;set;}
    public LocExtension(string key)=>Key=key;
    public override object ProvideValue(IServiceProvider serviceProvider)=>UiText.T(Key);
}
