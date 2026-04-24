using CsvImporter.Mapping;

namespace CsvImporter.Tests;

public class FuzzyMatcherTests
{
    [Theory]
    [InlineData("",      "",      0)]
    [InlineData("abc",   "",      3)]
    [InlineData("",      "abc",   3)]
    [InlineData("kitten","sitting",3)]
    [InlineData("abc",   "abc",   0)]
    public void LevenshteinDistance_KnownValues(string a, string b, int expected)
    {
        Assert.Equal(expected, FuzzyMatcher.LevenshteinDistance(a, b));
    }

    [Fact]
    public void FuzzyScore_IdenticalStrings_Returns1()
    {
        Assert.Equal(1.0, FuzzyMatcher.FuzzyScore("KUNDE", "KUNDE"));
    }

    [Fact]
    public void FuzzyScore_CompletelyDifferent_Returns0()
    {
        Assert.Equal(0.0, FuzzyMatcher.FuzzyScore("ABC", "XYZ"));
    }

    [Fact]
    public void FuzzyScore_IsNormalized_BetweenZeroAndOne()
    {
        double score = FuzzyMatcher.FuzzyScore("KUNDEN", "KUND");
        Assert.InRange(score, 0.0, 1.0);
    }

    [Fact]
    public void FuzzyScore_EmptyStrings_Returns1()
    {
        Assert.Equal(1.0, FuzzyMatcher.FuzzyScore("", ""));
    }
}
