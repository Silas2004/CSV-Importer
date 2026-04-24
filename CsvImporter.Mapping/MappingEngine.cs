using CsvImporter.Core.Models;

namespace CsvImporter.Mapping;

public class MappingEngine
{
    private readonly SynonymResolver _synonyms;
    private readonly double          _fuzzyThreshold;
    private readonly double          _vectorThreshold;

    public MappingEngine(SynonymResolver synonyms, double fuzzyThreshold = 0.75, double vectorThreshold = 0.70)
    {
        _synonyms        = synonyms;
        _fuzzyThreshold  = fuzzyThreshold;
        _vectorThreshold = vectorThreshold;
    }

    public Task<List<ColumnMapping>> RunAsync(
        List<CsvColumn> csvColumns,
        List<DbColumn>  dbColumns,
        Dictionary<string, string>? savedMappings = null)
    {
        var result = new List<ColumnMapping>(csvColumns.Count);

        foreach (var csv in csvColumns)
        {
            var mapping = new ColumnMapping { Source = csv };

            // Check saved mappings first
            if (savedMappings is not null &&
                savedMappings.TryGetValue(csv.Name, out var savedTarget))
            {
                var db = dbColumns.FirstOrDefault(d => d.Name.Equals(savedTarget, StringComparison.OrdinalIgnoreCase));
                if (db is not null)
                {
                    mapping.Target = db;
                    mapping.Method = MappingMethod.Manual;
                    mapping.Score  = 1.0;
                    mapping.Status = ScoreToStatus(1.0, mapping.HasTypeWarning);
                    result.Add(mapping);
                    continue;
                }
            }

            TryMatch(csv, dbColumns, mapping);
            result.Add(mapping);
        }

        return Task.FromResult(result);
    }

    private void TryMatch(CsvColumn csv, List<DbColumn> dbColumns, ColumnMapping mapping)
    {
        // Step 1: ExactMatch
        var exact = dbColumns.FirstOrDefault(d =>
            string.Equals(csv.NormalizedName, d.NormalizedName, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            Apply(mapping, exact, MappingMethod.ExactMatch, 1.0);
            return;
        }

        // Step 2: NormalizedMatch
        var csvNorm = TokenNormalizer.Normalize(csv.Name);
        var normMatch = dbColumns.FirstOrDefault(d =>
            string.Equals(csvNorm, TokenNormalizer.Normalize(d.Name), StringComparison.OrdinalIgnoreCase));
        if (normMatch is not null)
        {
            Apply(mapping, normMatch, MappingMethod.NormalizedMatch, 0.95);
            return;
        }

        // Step 3: TokenMatch
        var csvTokens = TokenNormalizer.Tokenize(csv.Name);
        var tokenMatch = dbColumns.FirstOrDefault(d =>
        {
            var dbTokens = TokenNormalizer.Tokenize(d.Name);
            return csvTokens.All(ct => dbTokens.Any(dt =>
                dt.Equals(ct, StringComparison.OrdinalIgnoreCase) ||
                (ct.Length >= 3 && dt.StartsWith(ct, StringComparison.OrdinalIgnoreCase))));
        });
        if (tokenMatch is not null)
        {
            Apply(mapping, tokenMatch, MappingMethod.TokenMatch, 0.90);
            return;
        }

        // Step 4: SynonymResolve
        var expandedTokens = csvTokens.Select(t => _synonyms.Resolve(t)).ToArray();
        var expandedNorm   = string.Join(string.Empty, expandedTokens);
        var synMatch = dbColumns.FirstOrDefault(d =>
            string.Equals(expandedNorm, TokenNormalizer.Normalize(d.Name), StringComparison.OrdinalIgnoreCase));
        if (synMatch is not null)
        {
            Apply(mapping, synMatch, MappingMethod.Synonym, 0.88);
            return;
        }

        // Step 5: FuzzyMatch — best score across all DB columns
        DbColumn? bestFuzzy = null;
        double bestFuzzyScore = 0;
        foreach (var db in dbColumns)
        {
            double score = FuzzyMatcher.FuzzyScore(csvNorm, TokenNormalizer.Normalize(db.Name));
            if (score > bestFuzzyScore) { bestFuzzyScore = score; bestFuzzy = db; }
        }
        if (bestFuzzy is not null && bestFuzzyScore >= _fuzzyThreshold)
        {
            Apply(mapping, bestFuzzy, MappingMethod.Fuzzy, bestFuzzyScore);
            return;
        }

        // Step 6: VectorMatch
        var csvVec = VectorMatcher.ToVector(csvNorm);
        DbColumn? bestVector = null;
        double bestVectorScore = 0;
        foreach (var db in dbColumns)
        {
            var dbVec = VectorMatcher.ToVector(TokenNormalizer.Normalize(db.Name));
            double score = VectorMatcher.CosineSimilarity(csvVec, dbVec);
            if (score > bestVectorScore) { bestVectorScore = score; bestVector = db; }
        }
        if (bestVector is not null && bestVectorScore >= _vectorThreshold)
        {
            Apply(mapping, bestVector, MappingMethod.Vector, bestVectorScore);
            return;
        }

        // Step 7: NoMatch
        mapping.Status = MappingStatus.Unmatched;
        mapping.Method = MappingMethod.None;
        mapping.Score  = bestFuzzyScore > bestVectorScore ? bestFuzzyScore : bestVectorScore;
    }

    private static void Apply(ColumnMapping mapping, DbColumn target, MappingMethod method, double score)
    {
        mapping.Target = target;
        mapping.Method = method;
        mapping.Score  = score;
        mapping.Status = ScoreToStatus(score, mapping.HasTypeWarning);
    }

    private static MappingStatus ScoreToStatus(double score, bool hasTypeWarning)
    {
        if (score >= 0.85 && !hasTypeWarning) return MappingStatus.Matched;
        if (score >= 0.50) return MappingStatus.Warned;
        return MappingStatus.Unmatched;
    }
}
