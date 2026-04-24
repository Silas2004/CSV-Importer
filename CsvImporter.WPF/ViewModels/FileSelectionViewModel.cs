using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CsvImporter.WPF.Models;
using Microsoft.Win32;

namespace CsvImporter.WPF.ViewModels;

public partial class FileSelectionViewModel : ObservableObject, IWizardStep
{
    private readonly ImportContext _context;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanProceed))]
    [NotifyPropertyChangedFor(nameof(TotalSummary))]
    private ObservableCollection<string> _selectedFiles = new();

    [ObservableProperty] private string _delimiterText  = ";";
    [ObservableProperty] private string _encodingName   = "UTF-8";

    public bool CanProceed => SelectedFiles.Count > 0;

    public Task EnterAsync() => Task.CompletedTask;

    public string TotalSummary
    {
        get
        {
            if (SelectedFiles.Count == 0) return "Keine Dateien ausgewählt";
            long total = SelectedFiles.Where(File.Exists).Sum(f => new FileInfo(f).Length);
            return $"{SelectedFiles.Count} Datei(en) — {total / 1024.0 / 1024.0:F1} MB";
        }
    }

    public FileSelectionViewModel(ImportContext context)
    {
        _context      = context;
        DelimiterText = context.Delimiter.ToString();
        EncodingName  = context.EncodingName;
    }

    [RelayCommand]
    private void AddFiles()
    {
        var dlg = new OpenFileDialog { Filter = "CSV-Dateien (*.csv)|*.csv", Multiselect = true };
        if (dlg.ShowDialog() != true) return;
        foreach (var path in dlg.FileNames)
            if (!SelectedFiles.Contains(path))
                SelectedFiles.Add(path);
        SyncToContext();
    }

    [RelayCommand]
    private void AddFolder()
    {
        var dlg = new OpenFolderDialog { Title = "Ordner mit CSV-Dateien auswählen" };
        if (dlg.ShowDialog() != true) return;
        foreach (var file in Directory.EnumerateFiles(dlg.FolderName, "*.csv", SearchOption.AllDirectories))
            if (!SelectedFiles.Contains(file))
                SelectedFiles.Add(file);
        SyncToContext();
    }

    [RelayCommand]
    private void RemoveFile(string path)
    {
        SelectedFiles.Remove(path);
        SyncToContext();
    }

    partial void OnDelimiterTextChanged(string value)
    {
        if (value.Length == 1) _context.Delimiter = value[0];
    }

    partial void OnEncodingNameChanged(string value)
    {
        _context.EncodingName = value;
    }

    private void SyncToContext()
    {
        _context.FilePaths = SelectedFiles.ToList();
        OnPropertyChanged(nameof(TotalSummary));
        OnPropertyChanged(nameof(CanProceed));
    }
}
