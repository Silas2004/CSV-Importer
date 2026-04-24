using CsvImporter.Mapping;

namespace CsvImporter.Tests;

public class VectorMatcherTests
{
    [Fact]
    public void ToVector_ProducesTrigramsForKnownWord()
    {
        var vec = VectorMatcher.ToVector("ABC");
        Assert.True(vec.ContainsKey("##A"));
        Assert.True(vec.ContainsKey("#AB"));
        Assert.True(vec.ContainsKey("ABC"));
        Assert.True(vec.ContainsKey("BC#"));
        Assert.True(vec.ContainsKey("C##"));
    }

    [Fact]
    public void CosineSimilarity_IdenticalVectors_Returns1()
    {
        var v = VectorMatcher.ToVector("KUNDE");
        Assert.Equal(1.0, VectorMatcher.CosineSimilarity(v, v), precision: 10);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_Returns0()
    {
        var v1 = new Dictionary<string, int> { ["AAA"] = 1 };
        var v2 = new Dictionary<string, int> { ["ZZZ"] = 1 };
        Assert.Equal(0.0, VectorMatcher.CosineSimilarity(v1, v2));
    }

    [Fact]
    public void CosineSimilarity_SimilarWords_HighScore()
    {
        var v1 = VectorMatcher.ToVector("KUNDE");
        var v2 = VectorMatcher.ToVector("KUNDEN");
        double score = VectorMatcher.CosineSimilarity(v1, v2);
        Assert.True(score > 0.6, $"Expected score > 0.6 but got {score}");
    }
}
