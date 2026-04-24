namespace CsvImporter.Mapping;

public class SynonymResolver
{
    private readonly Dictionary<string, string> _synonyms;

    public SynonymResolver(Dictionary<string, string> synonyms)
    {
        _synonyms = new Dictionary<string, string>(synonyms, StringComparer.OrdinalIgnoreCase);
    }

    public string Resolve(string token)
    {
        if (token.Length <= 4 && _synonyms.TryGetValue(token, out var expanded))
            return expanded;
        return token;
    }
}
