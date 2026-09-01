using Mars.Plugin.Contracts.Plugins;
using Mars.Plugin.Services;
using Mars.Server.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Mars.Plugin.Handlers;

/// <summary>
/// Общий финал установки: перенос стейджинга в `plugins/&lt;PackageId&gt;/`.
/// Если существующая папка удерживается загруженной сборкой работающего плагина
/// (удаление падает), подмена откладывается до рестарта: стейджинг переименовывается
/// в `_pending_&lt;PackageId&gt;_&lt;guid&gt;`, отметка пишется в реестр.
/// </summary>
internal static class PluginInstallFinalizer
{
    /// <returns>путь окончательной папки плагина (относительно data)</returns>
    internal static async Task<string> FinalizeAsync(
        IFileStorage fileStorage, PluginRegistry registry, ILogger logger,
        string stagingDir, string packageId, PluginSource source, string version,
        Func<string, string, Task> moveAsync)
    {
        var finalDir = Path.Combine(PluginManager.PluginsDefaultPath, packageId);
        if (fileStorage.DirectoryExists(finalDir))
        {
            try
            {
                logger.LogInformation("Replacing existing plugin folder '{Dir}'", finalDir);
                fileStorage.DeleteDirectory(finalDir, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                var pendingDir = Path.Combine(PluginManager.PluginsDefaultPath, $"_pending_{packageId}_{Guid.NewGuid():N}");
                await moveAsync(stagingDir, pendingDir);
                registry.MarkInstalled(packageId, source, version, DateTimeOffset.UtcNow, pendingStagingDir: pendingDir);
                logger.LogInformation("Plugin '{PackageId}' folder is locked by the running assembly — replacement deferred until restart ('{Dir}').", packageId, pendingDir);
                return finalDir;
            }
        }

        await moveAsync(stagingDir, finalDir);
        registry.MarkInstalled(packageId, source, version, DateTimeOffset.UtcNow);
        return finalDir;
    }
}
