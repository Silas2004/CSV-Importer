using System.Text;
using CsvImporter.Core.Models;

namespace CsvImporter.Core.Interfaces;

public interface IImportStrategy
{
    ImportStrategy StrategyType { get; }
    IEnumerable<List<string[]>> ReadBatches(string filePath, string delimiter, Encoding encoding);
}
