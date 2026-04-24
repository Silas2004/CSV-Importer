using System.Windows;
using CsvImporter.WPF.Models;
using CsvImporter.WPF.ViewModels;
using CsvImporter.WPF.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CsvImporter.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var context = App.Services.GetRequiredService<ImportContext>();
        if (string.IsNullOrWhiteSpace(context.Profile?.Host))
            OpenSettings_Click(this, new RoutedEventArgs());
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        var connectionVm = App.Services.GetRequiredService<ConnectionViewModel>();
        var context      = App.Services.GetRequiredService<ImportContext>();
        var win = new SettingsWindow(connectionVm, context)
        {
            Owner = this
        };
        win.ShowDialog();
    }
}
