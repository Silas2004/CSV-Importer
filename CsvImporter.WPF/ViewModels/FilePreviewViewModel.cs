using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CsvImporter.WPF.Models;

namespace CsvImporter.WPF.ViewModels;

public partial class FilePreviewViewModel : ObservableObject, IWizardStep
{
    private readonly ImportContext _context;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreview))]
    private DataView? _preview;

    [ObservableProperty] private string? _selectedFile;

    public List<string> FilePaths        => _context.FilePaths;
    public bool         HasPreview       => Preview is not null;
    public bool         HasMultipleFiles => _context.FilePaths.Count > 1;
    public bool         CanProceed       => true;

    public FilePreviewViewModel(ImportContext context) => _context = context;

    public Task EnterAsync()
    {
        OnPropertyChanged(nameof(FilePaths));
        SelectedFile = _context.FilePaths.FirstOrDefault();
        if (SelectedFile is not null)
            LoadPreview(SelectedFile);
        return Task.CompletedTask;
    }

    partial void OnSelectedFileChanged(string? value)
    {
        if (value is not null)
            LoadPreview(value);
    }

    private void LoadPreview(string path)
    {
        try
        {
            using var reader = new System.IO.StreamReader(path, _context.Encoding, detectEncodingFromByteOrderMarks: true);
            var headerLine = reader.ReadLine();
            if (headerLine is null) { Preview = null; return; }

            var headers = headerLine.Split(_context.Delimiter)
                                    .Select(h => h.Trim('"').Trim())
                                    .ToArray();
            _context.CsvHeaders = headers.ToList();

            var dt = new DataTable();
            foreach (var h in headers)
                dt.Columns.Add(h, typeof(string));

            var rows = new List<string[]>();
            for (int i = 0; i < 20; i++)
            {
                var line = reader.ReadLine();
                if (line is null) break;
                var cells = line.Split(_context.Delimiter).Select(v => v.Trim('"')).ToArray();
                rows.Add(cells);
                var row = dt.NewRow();
                for (int c = 0; c < headers.Length; c++)
                    row[c] = c < cells.Length ? cells[c] : string.Empty;
                dt.Rows.Add(row);
            }
            _context.PreviewRows = rows;
            Preview = dt.DefaultView;
        }
        catch
        {
            Preview = null;
        }
    }
}
