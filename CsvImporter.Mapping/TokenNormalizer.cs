using System.Text.RegularExpressions;

namespace CsvImporter.Mapping;

public static class TokenNormalizer
{
    private static readonly Regex SpecialChars = new(@"[^A-Z0-9]", RegexOptions.Compiled);
    private static readonly Regex CamelCase    = new(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);

    public static string Normalize(string name)
    {
        var upper = name.ToUpperInvariant();
        return SpecialChars.Replace(upper, string.Empty);
    }

    public static string[] Tokenize(string name)
    {
        var withSpaces = CamelCase.Replace(name, " ");
        return withSpaces.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(t => t.ToUpperInvariant())
                         .ToArray();
    }
}
