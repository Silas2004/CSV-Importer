namespace CsvImporter.WPF.ViewModels;

public interface IWizardStep
{
    Task EnterAsync();
    bool CanProceed { get; }
}
