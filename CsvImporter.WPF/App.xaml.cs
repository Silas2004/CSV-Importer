using System.Windows;
using CsvImporter.Adapters;
using CsvImporter.Core.Interfaces;
using CsvImporter.Core.Models;
using CsvImporter.Core.Services;
using CsvImporter.FileSystem;
using CsvImporter.FileSystem.Strategy;
using CsvImporter.WPF.Models;
using CsvImporter.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CsvImporter.WPF;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        RegisterServices(services);
        Services = services.BuildServiceProvider();

        var settingsSvc = Services.GetRequiredService<AppSettingsService>();
        settingsSvc.Load();

        var ctx = Services.GetRequiredService<ImportContext>();
        var defaultProfile = settingsSvc.Connections
            .FirstOrDefault(c => c.IsDefault)
            ?? settingsSvc.Connections.FirstOrDefault();
        if (defaultProfile is not null)
            ctx.Profile = defaultProfile;

        var wizard = Services.GetRequiredService<WizardViewModel>();
        var window = new MainWindow { DataContext = wizard };
        window.Show();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Core singletons
        services.AddSingleton<CredentialService>();
        services.AddSingleton<AppSettingsService>(sp =>
            new AppSettingsService(sp.GetRequiredService<CredentialService>()));
        services.AddSingleton<ImportQueue>();
        services.AddSingleton<ImportOrchestrator>(sp =>
        {
            var settings = sp.GetRequiredService<AppSettingsService>();
            return new ImportOrchestrator(
                provider => DbAdapterFactory.Create(provider),
                job      => new ImportStrategyResolver(settings.Current.Import.SizeThresholdBytes).Resolve(job),
                settings.Current.Import.MaxParallelImports,
                settings.Current.Import.SizeThresholdBytes);
        });

        // Shared wizard state (singleton so all steps see the same data)
        services.AddSingleton<ImportContext>();

        // ViewModels
        services.AddTransient<ConnectionViewModel>();
        services.AddTransient<TableSelectionViewModel>();
        services.AddTransient<FileSelectionViewModel>();
        services.AddTransient<FilePreviewViewModel>();
        services.AddTransient<MappingViewModel>();
        services.AddTransient<ImportProgressViewModel>();
        services.AddTransient<WizardViewModel>();
    }
}
