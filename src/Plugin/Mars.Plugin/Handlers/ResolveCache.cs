using System.Text.Json;
using Mars.Server.Abstractions.Services;

namespace Mars.Plugin.Handlers;

/// <summary>
/// Кэш резолва плавающих версий зависимостей (`&lt;packageId&gt;|&lt;range&gt;` → версия):
/// повторные установки не спрашивают фид «какая версия подходит» заново.
/// Живёт в `data/plugins/.resolve-cache.json`, записи протухают по TTL.
/// </summary>
internal sealed class ResolveCache
{
    internal const string FileName = ".resolve-cache.json";
    static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
    static readonly object WriteLock = new();

    internal record Entry(string Version, int SourceIndex, DateTimeOffset ResolvedAt);

    readonly Dictionary<string, Entry> _entries;
    bool _dirty;

    ResolveCache(Dictionary<string, Entry> entries) => _entries = entries;

    internal static ResolveCache Load(IFileStorage fileStorage, string path)
    {
        try
        {
            if (fileStorage.FileExists(path))
            {
                var entries = JsonSerializer.Deserialize<Dictionary<string, Entry>>(fileStorage.ReadAllText(path));
                if (entries is not null)
                {
                    var cutoff = DateTimeOffset.UtcNow - Ttl;
                    foreach (var stale in entries.Where(kv => kv.Value.ResolvedAt < cutoff).Select(kv => kv.Key).ToList())
                        entries.Remove(stale);
                    return new ResolveCache(entries);
                }
            }
        }
        catch (JsonException)
        {
            // битый кэш — начинаем с пустого
        }

        return new ResolveCache(new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase));
    }

    internal bool TryGet(string key, out Entry entry) => _entries.TryGetValue(key, out entry!);

    internal void Set(string key, string version, int sourceIndex)
    {
        _entries[key] = new Entry(version, sourceIndex, DateTimeOffset.UtcNow);
        _dirty = true;
    }

    internal void Save(IFileStorage fileStorage, string path)
    {
        if (!_dirty) return;

        lock (WriteLock)
        {
            var dir = Path.GetDirectoryName(path);
            if (dir is not null && !fileStorage.DirectoryExists(dir))
                fileStorage.CreateDirectory(dir);
            fileStorage.Write(path, JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
