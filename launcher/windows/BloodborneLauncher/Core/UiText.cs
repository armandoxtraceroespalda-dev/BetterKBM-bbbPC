using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace BloodborneLauncher.Core;

/// <summary>Only the active launcher catalog is retained. Game text owns the selection; voices stay independent.</summary>
public static class UiText
{
    private static IReadOnlyDictionary<string,string> _messages=new Dictionary<string,string>();
    public static string Locale {get;private set;}="en";
    public static void UseTextLanguage(string value)
    {
        var next=value switch {"0"=>"ja","2"=>"fr","3"=>"es-ES","20"=>"es-MX","4"=>"de","5"=>"it","8"=>"ru","17"=>"pt-BR",_=>"en"};
        if(next==Locale)return;
        Locale=next;
        string catalog=Locale.StartsWith("es",StringComparison.Ordinal)?"es":Locale;
        if(catalog=="en") {_messages=new Dictionary<string,string>();return;}
        using var stream=typeof(UiText).Assembly.GetManifestResourceStream("BloodborneLauncher.Localization."+catalog+".json");
        _messages=stream==null?new Dictionary<string,string>():JsonSerializer.Deserialize<Dictionary<string,string>>(stream)??new();
    }
    public static string T(string? source)=>source==null?"":_messages.TryGetValue(source,out var value)?value:source;
    public static object? T(object? source)=>source is string text?T(text):source;
    public static string F(FormattableString source)=>Format(source.Format,source.GetArguments());
    public static string Format(string format,params object?[] values)
    {
        try{return string.Format(CultureInfo.GetCultureInfo(Locale),T(format),values);}
        catch(FormatException){return string.Format(CultureInfo.InvariantCulture,format,values);}
    }
}
