using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CsvImporter.Adapters;
using CsvImporter.Core.Models;
using CsvImporter.Core.Services;

namespace CsvImporter.WPF.ViewModels;

public partial class ConnectionViewModel : ObservableObject
{
    private readonly AppSettingsService _settings;

    [ObservableProperty] private ConnectionProfile _currentProfile = new();
    [ObservableProperty] private bool _isTesting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTestResult))]
    private string? _connectionTestResult;

    [ObservableProperty] private bool _testSuccess;

    public bool HasTestResult => ConnectionTestResult is not null;

    public ObservableCollection<ConnectionProfile> Profiles { get; } = new();

    public ConnectionViewModel(AppSettingsService settings)
    {
        _settings = settings;

        foreach (var p in _settings.Connections)
            Profiles.Add(p);

        CurrentProfile = Profiles.FirstOrDefault(p => p.IsDefault)
                      ?? Profiles.FirstOrDefault()
                      ?? new ConnectionProfile();
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        ConnectionTestResult = null;
        try
        {
            await using var adapter = DbAdapterFactory.Create(CurrentProfile.Provider);
            var ok = await adapter.TestConnectionAsync(CurrentProfile);
            TestSuccess = ok;
            ConnectionTestResult = ok ? "Connection successful." : "Connection failed.";
        }
        catch (Exception ex)
        {
            TestSuccess = false;
            ConnectionTestResult = $"Error: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private void SaveProfile()
    {
        if (string.IsNullOrWhiteSpace(CurrentProfile.Name))
            CurrentProfile.Name = $"Profil {Profiles.Count + 1}";

        // Service handles Add/Update + persistence. The returned instance is the
        // one actually tracked — make the VM point at it so further edits stay in sync.
        var tracked = _settings.UpsertConnection(CurrentProfile);

        if (!Profiles.Contains(tracked))
            Profiles.Add(tracked);

        CurrentProfile = tracked;
    }

    [RelayCommand]
    private void NewProfile() => CurrentProfile = new ConnectionProfile();

    [RelayCommand]
    private void DeleteProfile()
    {
        if (!_settings.RemoveConnection(CurrentProfile.Id))
            return;

        var toRemove = Profiles.FirstOrDefault(p => p.Id == CurrentProfile.Id);
        if (toRemove is not null)
            Profiles.Remove(toRemove);

        CurrentProfile = Profiles.Count > 0 ? Profiles[0] : new ConnectionProfile();
    }

    partial void OnCurrentProfileChanged(ConnectionProfile value)
    {
        ConnectionTestResult = null;
        TestSuccess = false;
    }
}