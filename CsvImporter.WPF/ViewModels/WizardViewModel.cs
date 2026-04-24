using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CsvImporter.WPF.ViewModels;

public partial class WizardViewModel : ObservableObject
{
    private readonly IReadOnlyList<ObservableObject> _steps;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentStepViewModel))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    private int _currentStep = 0;

    public static string[] StepLabels { get; } =
        { "Tabelle", "Dateien", "Vorschau", "Mapping", "Import" };

    public WizardViewModel(
        TableSelectionViewModel  tableSelection,
        FileSelectionViewModel   fileSelection,
        FilePreviewViewModel     filePreview,
        MappingViewModel         mapping,
        ImportProgressViewModel  importProgress)
    {
        _steps = new ObservableObject[]
        {
            tableSelection, fileSelection, filePreview, mapping, importProgress
        };

        // Bubble any step's CanProceed change into NextCommand re-evaluation
        foreach (var step in _steps.OfType<INotifyPropertyChanged>())
            step.PropertyChanged += OnAnyStepPropertyChanged;

        if (_steps[0] is IWizardStep first)
            _ = first.EnterAsync();
    }

    public ObservableObject? CurrentStepViewModel
        => CurrentStep >= 0 && CurrentStep < _steps.Count ? _steps[CurrentStep] : null;

    public int StepCount => _steps.Count;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task Next()
    {
        CurrentStep++;
        if (_steps[CurrentStep] is IWizardStep step)
            await step.EnterAsync();
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back() => CurrentStep--;

    private bool CanGoNext()
    {
        if (CurrentStep >= _steps.Count - 1) return false;
        if (_steps[CurrentStep] is IWizardStep step) return step.CanProceed;
        return true;
    }

    private bool CanGoBack() => CurrentStep > 0;

    private void OnAnyStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is "CanProceed")
            NextCommand.NotifyCanExecuteChanged();
    }
}
