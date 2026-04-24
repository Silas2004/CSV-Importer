using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsvImporter.Core.Models;

namespace CsvImporter.Core.Services;

public class AppSettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    private readonly CredentialService _credentials;
    private readonly List<ConnectionProfile> _connections = new();

    private AppSettings _settings = new();

    public AppSettingsService(CredentialService credentials, string? settingsPath = null)
    {
        _credentials = credentials;
        _path = settingsPath ?? GetDefaultSettingsPath();
    }

    public AppSettings Current => _settings;

    /// <summary>
    /// Read-only view of the connections. Mutations must go through
    /// <see cref="UpsertConnection"/> / <see cref="RemoveConnection"/>
    /// so the service stays the single source of truth.
    /// </summary>
    public IReadOnlyList<ConnectionProfile> Connections => _connections;

    public void Load()
    {
        if (!File.Exists(_path))
        {
            _settings = new AppSettings();
            _connections.Clear();
            Debug.WriteLine($"[Settings] No file at {_path}, using defaults.");
            return;
        }

        try
        {
            var json = File.ReadAllText(_path);
            var raw = JsonSerializer.Deserialize<AppSettingsRaw>(json, JsonOpts)
                       ?? new AppSettingsRaw();

            _settings = new AppSettings
            {
                Import = raw.Import ?? new ImportSettings(),
                Mapping = raw.Mapping ?? new MappingSettings(),
            };

            _connections.Clear();
            foreach (var c in raw.Connections ?? new List<ConnectionProfileRaw>())
            {
                _connections.Add(new ConnectionProfile
                {
                    Id = c.Id == Guid.Empty ? Guid.NewGuid() : c.Id,
                    Name = c.Name,
                    Provider = c.Provider,
                    Host = c.Host,
                    Port = c.Port,
                    ServiceName = c.ServiceName,
                    DbName = c.DbName,
                    Username = c.Username,
                    Password = TryDecrypt(c.Password),
                    Role = c.Role,
                    IsDefault = c.IsDefault,
                });
            }

            Debug.WriteLine($"[Settings] Loaded {_connections.Count} connection(s) from {_path}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Load failed: {ex}");
            _settings = new AppSettings();
            _connections.Clear();
        }
    }

    public void Save()
    {
        var raw = new AppSettingsRaw
        {
            Import = _settings.Import,
            Mapping = _settings.Mapping,
            Connections = _connections.Select(c => new ConnectionProfileRaw
            {
                Id = c.Id,
                Name = c.Name,
                Provider = c.Provider,
                Host = c.Host,
                Port = c.Port,
                ServiceName = c.ServiceName,
                DbName = c.DbName,
                Username = c.Username,
                Password = TryEncrypt(c.Password),
                Role = c.Role,
                IsDefault = c.IsDefault,
            }).ToList()
        };

        var json = JsonSerializer.Serialize(raw, JsonOpts);

        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // Atomic write: write to temp file, then replace. Avoids a corrupted
            // settings file if the process is killed mid-write.
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, json);

            if (File.Exists(_path))
                File.Replace(tmp, _path, destinationBackupFileName: null);
            else
                File.Move(tmp, _path);

            Debug.WriteLine($"[Settings] Saved {_connections.Count} connection(s) to {_path}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Save failed: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Add or update a profile (matched by <see cref="ConnectionProfile.Id"/>) and persist.
    /// Returns the tracked instance (same reference as passed in if it was added,
    /// or the existing instance with updated fields).
    /// </summary>
    public ConnectionProfile UpsertConnection(ConnectionProfile profile)
    {
        if (profile.Id == Guid.Empty)
            profile.Id = Guid.NewGuid();

        var existing = _connections.FirstOrDefault(p => p.Id == profile.Id);
        if (existing is null)
        {
            _connections.Add(profile);
            existing = profile;
        }
        else if (!ReferenceEquals(existing, profile))
        {
            CopyFields(profile, existing);
        }

        // Enforce single default
        if (existing.IsDefault)
        {
            foreach (var other in _connections)
                if (!ReferenceEquals(other, existing))
                    other.IsDefault = false;
        }

        Save();
        return existing;
    }

    public bool RemoveConnection(Guid id)
    {
        var existing = _connections.FirstOrDefault(p => p.Id == id);
        if (existing is null) return false;

        _connections.Remove(existing);
        Save();
        return true;
    }

    private static void CopyFields(ConnectionProfile src, ConnectionProfile dst)
    {
        dst.Name = src.Name;
        dst.Provider = src.Provider;
        dst.Host = src.Host;
        dst.Port = src.Port;
        dst.ServiceName = src.ServiceName;
        dst.DbName = src.DbName;
        dst.Username = src.Username;
        dst.Password = src.Password;
        dst.Role = src.Role;
        dst.IsDefault = src.IsDefault;
    }

    private static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "CsvImporter", "appsettings.json");
    }

    private string TryEncrypt(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return plain;
        try { return _credentials.Encrypt(plain); }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Encrypt failed, storing plain: {ex.Message}");
            return plain;
        }
    }

    private string TryDecrypt(string cipher)
    {
        if (string.IsNullOrEmpty(cipher)) return cipher;
        try { return _credentials.Decrypt(cipher); }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Decrypt failed, returning raw: {ex.Message}");
            return cipher;
        }
    }

    // Raw DTOs for JSON (flat list form)
    private sealed class AppSettingsRaw
    {
        public List<ConnectionProfileRaw>? Connections { get; set; }
        public ImportSettings? Import { get; set; }
        public MappingSettings? Mapping { get; set; }
    }

    private sealed class ConnectionProfileRaw
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DbProvider Provider { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string DbName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DbRole Role { get; set; }
        public bool IsDefault { get; set; }
    }
}