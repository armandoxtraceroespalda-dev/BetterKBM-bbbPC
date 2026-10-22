using System.Text.RegularExpressions;

namespace BloodborneLauncher.Core;

/// <summary>Clean display labels only. Raw XML names remain the persistent patch identifiers.</summary>
public static class PatchTitles
{
    private static readonly Regex Notes = new(@"\s*[\(\[（]\s*(?:read\s+(?:the\s+)?notes?|(?:lee|lea|leer)\s+(?:las?\s+)?notas?|lire\s+(?:les?\s+)?notes?|hinweise\s+lesen|(?:leggi|leggere)\s+(?:le\s+)?note|leia\s+(?:as?\s+)?notas?|читать\s+примечания|см\.?\s+примечания|注記を読む|注意事項を読む|请阅读说明|阅读说明)\s*[\)\]）]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    public static string Clean(string text) => Notes.Replace(text, "").Trim();
    public static string Display(string raw, Func<string, string> translate)
    {
        string translated = translate(raw);
        if (translated == raw) translated = translate(Clean(raw));
        return Clean(translated);
    }
}
