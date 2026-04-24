using CsvImporter.Core.Models;
using CsvImporter.Mapping;

namespace CsvImporter.Tests;

public class MappingEngineTests
{
    private static MappingEngine BuildEngine(Dictionary<string, string>? synonyms = null)
    {
        var resolver = new SynonymResolver(synonyms ?? new Dictionary<string, string>());
        return new MappingEngine(resolver, fuzzyThreshold: 0.75, vectorThreshold: 0.70);
    }

    private static CsvColumn Csv(string name, int idx = 0) => new()
    {
        Index          = idx,
        Name           = name,
        NormalizedName = name.ToUpperInvariant(),
    };

    private static DbColumn Db(string name) => new()
    {
        Name           = name,
        NormalizedName = name.ToUpperInvariant(),
        DataType       = "VARCHAR2",
        IsNullable     = true,
    };

    [Fact]
    public async Task ExactMatch_ReturnsMatchedStatus()
    {
        var engine  = BuildEngine();
        var csvCols = new List<CsvColumn> { Csv("KUNDE_ID") };
        var dbCols  = new List<DbColumn>  { Db("KUNDE_ID") };

        var result = await engine.RunAsync(csvCols, dbCols);

        Assert.Single(result);
        Assert.Equal(MappingMethod.ExactMatch, result[0].Method);
        Assert.Equal(MappingStatus.Matched, result[0].Status);
    }

    [Fact]
    public async Task NoDbMatch_ReturnsUnmatched()
    {
        var engine  = BuildEngine();
        var csvCols = new List<CsvColumn> { Csv("TOTALLY_DIFFERENT_COLUMN") };
        var dbCols  = new List<DbColumn>  { Db("AAAAA") };

        var result = await engine.RunAsync(csvCols, dbCols);

        Assert.Equal(MappingStatus.Unmatched, result[0].Status);
    }

    [Fact]
    public async Task SynonymExpansion_MatchesExpandedName()
    {
        var synonyms = new Dictionary<string, string> { ["NR"] = "NUMMER" };
        var engine   = BuildEngine(synonyms);
        var csvCols  = new List<CsvColumn> { Csv("KUNDEN_NR") };
        var dbCols   = new List<DbColumn>  { Db("KUNDENNUMMER") };

        var result = await engine.RunAsync(csvCols, dbCols);

        // Should match via Synonym or Fuzzy
        Assert.NotEqual(MappingStatus.Unmatched, result[0].Status);
    }

    [Fact]
    public async Task FuzzyMatch_CloseName_Matched()
    {
        var engine  = BuildEngine();
        var csvCols = new List<CsvColumn> { Csv("KUNDENNAME") };
        var dbCols  = new List<DbColumn>  { Db("KUNDENNAM") };

        var result = await engine.RunAsync(csvCols, dbCols);

        Assert.NotEqual(MappingStatus.Unmatched, result[0].Status);
        Assert.True(result[0].Score >= 0.75, $"Expected score >= 0.75, got {result[0].Score}");
    }

    [Fact]
    public async Task SavedMappings_OverridesAutoMatch()
    {
        var engine  = BuildEngine();
        var csvCols = new List<CsvColumn> { Csv("ID") };
        var dbCols  = new List<DbColumn>  { Db("KUNDE_ID"), Db("SOME_OTHER") };
        var saved   = new Dictionary<string, string> { ["ID"] = "SOME_OTHER" };

        var result = await engine.RunAsync(csvCols, dbCols, saved);

        Assert.Equal("SOME_OTHER", result[0].Target?.Name);
        Assert.Equal(MappingMethod.Manual, result[0].Method);
    }

    [Fact]
    public async Task ScoreAbove85_NoTypeWarning_IsMatched()
    {
        var engine  = BuildEngine();
        var csvCols = new List<CsvColumn> { Csv("NAME") };
        var dbCols  = new List<DbColumn>  { Db("NAME") };

        var result = await engine.RunAsync(csvCols, dbCols);

        Assert.Equal(MappingStatus.Matched, result[0].Status);
        Assert.True(result[0].Score >= 0.85);
    }

    [Fact]
    public async Task EmptyDbColumns_AllUnmatched()
    {
        var engine  = BuildEngine();
        var csvCols = new List<CsvColumn> { Csv("ID"), Csv("NAME") };
        var dbCols  = new List<DbColumn>();

        var result = await engine.RunAsync(csvCols, dbCols);

        Assert.All(result, m => Assert.Equal(MappingStatus.Unmatched, m.Status));
    }
}
