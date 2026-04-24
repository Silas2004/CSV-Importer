namespace CsvImporter.Mapping;

public static class VectorMatcher
{
    public static Dictionary<string, int> ToVector(string name, int n = 3)
    {
        var padded = new string('#', n - 1) + name + new string('#', n - 1);
        var freq = new Dictionary<string, int>();
        for (int i = 0; i <= padded.Length - n; i++)
        {
            var gram = padded.Substring(i, n);
            freq[gram] = freq.TryGetValue(gram, out var c) ? c + 1 : 1;
        }
        return freq;
    }

    public static double CosineSimilarity(Dictionary<string, int> v1, Dictionary<string, int> v2)
    {
        double dot = 0, mag1 = 0, mag2 = 0;

        foreach (var (key, val) in v1)
        {
            mag1 += (double)val * val;
            if (v2.TryGetValue(key, out var v2Val))
                dot += (double)val * v2Val;
        }
        foreach (var val in v2.Values)
            mag2 += (double)val * val;

        if (mag1 == 0 || mag2 == 0) return 0;
        return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
    }
}
