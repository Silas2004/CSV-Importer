using CsvImporter.Mapping;

namespace CsvImporter.Tests;

public class TokenNormalizerTests
{
    [Theory]
    [InlineData("CustomerName",    new[] { "CUSTOMER", "NAME" })]
    [InlineData("customer_name",   new[] { "CUSTOMER", "NAME" })]
    [InlineData("KUNDEN-NR",       new[] { "KUNDEN", "NR" })]
    [InlineData("PLZ",             new[] { "PLZ" })]
    [InlineData("GebDatum",        new[] { "GEB", "DATUM" })]
    public void Tokenize_SplitsCorrectly(string input, string[] expected)
    {
        var tokens = TokenNormalizer.Tokenize(input);
        Assert.Equal(expected, tokens);
    }

    [Theory]
    [InlineData("Customer_Name!", "CUSTOMERNAME")]
    [InlineData("plz",            "PLZ")]
    [InlineData("Straße",         "STRAE")]   // ß stripped (non-ASCII)
    [InlineData("Tel.Nr",         "TELNR")]
    public void Normalize_UppercasesAndStripsSpecials(string input, string expected)
    {
        var result = TokenNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }
}
