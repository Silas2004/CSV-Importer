using CommunityToolkit.Mvvm.ComponentModel;
using CsvImporter.Core.Models;
using System.Text;
using CsvImporter.WPF.Models;

namespace CsvImporter.WPF.Models;

public partial class ImportContext : ObservableObject
{
    [ObservableProperty] private ConnectionProfile _profile         = new();
    [ObservableProperty] private string?           _selectedTable;
    [ObservableProperty] private List<DbColumn>    _dbColumns       = new();
    [ObservableProperty] private List<string>      _filePaths       = new();
    [ObservableProperty] private char              _delimiter       = ',';
    [ObservableProperty] private string            _encodingName    = "UTF-8";
    [ObservableProperty] private List<string>      _csvHeaders      = new();
    [ObservableProperty] private List<string[]>    _previewRows     = new();
    [ObservableProperty] private List<MappingRow>  _mappingRows     = new();
    [ObservableProperty] private TransactionMode   _txMode          = TransactionMode.AllOrNothing;
    [ObservableProperty] private ErrorBehavior     _onError         = ErrorBehavior.Abort;
    [ObservableProperty] private int               _batchSize       = 100;

    public Encoding Encoding
    {
        get
        {
            try   { return Encoding.GetEncoding(EncodingName); }
            catch { return Encoding.UTF8; }
        }
    }
}
