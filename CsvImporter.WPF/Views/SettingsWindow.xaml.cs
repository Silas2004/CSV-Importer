using System.Windows;
using CsvImporter.WPF.Models;
using CsvImporter.WPF.ViewModels;

namespace CsvImporter.WPF.Views;

public partial class SettingsWindow : Window
{
    private readonly ImportContext      _context;
    private readonly ConnectionViewModel _vm;

    public SettingsWindow(ConnectionViewModel connectionVm, ImportContext context)
    {
        InitializeComponent();
        _vm      = connectionVm;
        _context = context;
        DataContext = connectionVm;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _context.Profile = _vm.CurrentProfile;
        Close();
    }
}
