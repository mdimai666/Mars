using System.Text.Json;
using Mars.Plugin.Contracts.Plugins;
using Mars.Plugin.Services;
using Mars.Server.Abstractions.Services;

namespace Mars.Plugin.Handlers;

/// <summary>
/// Реестр установленных плагинов (`data/plugins/.registry.json`): источник, версия,
/// дата установки, отключён, отложенные удаление/подмена папки. Читается и пишется
/// через IFileStorage до/после Build.
/// </summary>
internal class PluginRegistry
{
    const string FileName = ".registry.json";

    private readonly IFileStorage _fileStorage;
    private readonly string _dir;
    private Dictionary<string, PluginRegistryEntry> _entries;

    public PluginRegistry(IFileStorage fileStorage, string dir = PluginManager.PluginsDefaultPath)
    {
        _fileStorage = fileStorage;
        _dir = dir;
        _entries = Load();
    }

    public IReadOnlyDictionary<string, PluginRegistryEntry> Entries => _entries;

    public PluginRegistryEntry? Get(string packageId)
        => _entries.TryGetValue(packageId, out var entry) ? entry : null;

    public bool IsDisabled(string packageId)
        => _entries.TryGetValue(packageId, out var entry) && entry.Disabled;

    public void MarkInstalled(string packageId, PluginSource source, string version, DateTimeOffset installedAtUtc, string? pendingStagingDir = null)
    {
        _entries[packageId] = new PluginRegistryEntry
        {
            Source = source,
            Version = version,
            InstalledAtUtc = installedAtUtc,
            Disabled = Get(packageId)?.Disabled ?? false,
            PendingStagingDir = pendingStagingDir,
        };
        Save();
    }

    /// <summary>Отмечает плагин к удалению: папка и запись чистятся при следующем старте.</summary>
    public void MarkPendingDelete(string packageId)
    {
        var entry = Get(packageId);
        if (entry is null) return;
        _entries[packageId] = entry with { PendingDelete = true };
        Save();
    }

    /// <summary>Снимает отложенные отметки (применены при старте).</summary>
    public void ClearPendingMarks(string packageId)
    {
        var entry = Get(packageId);
        if (entry is null) return;
        _entries[packageId] = entry with { PendingDelete = false, PendingStagingDir = null };
        Save();
    }

    public void SetDisabled(string packageId, bool disabled)
    {
        var entry = Get(packageId);
        if (entry is null) return;
        _entries[packageId] = entry with { Disabled = disabled };
        Save();
    }

    public void Remove(string packageId)
    {
        if (!_entries.Remove(packageId)) return;
        Save();
    }

    Dictionary<string, PluginRegistryEntry> Load()
    {
        var path = Path.Combine(_dir, FileName);
        if (!_fileStorage.FileExists(path)) return [];

        try
        {
            _fileStorage.Read(path, out var stream);
            using (stream)
                return JsonSerializer.Deserialize<Dictionary<string, PluginRegistryEntry>>(stream) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    void Save()
    {
        var path = Path.Combine(_dir, FileName);
        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, _entries, new JsonSerializerOptions { WriteIndented = true });
        stream.Position = 0;
        _fileStorage.WriteAsync(path, stream, CancellationToken.None).GetAwaiter().GetResult();
    }
}

internal record PluginRegistryEntry
{
    public PluginSource Source { get; init; }
    public string Version { get; init; } = string.Empty;
    public DateTimeOffset InstalledAtUtc { get; init; }
    public bool Disabled { get; init; }

    /// <summary>Плагин отмечен к удалению: папка и запись чистятся при следующем старте.</summary>
    public bool PendingDelete { get; init; }

    /// <summary>Стейджинг новой версии (`plugins/_pending_...`), подменяющий папку при следующем старте.</summary>
    public string? PendingStagingDir { get; init; }
}
