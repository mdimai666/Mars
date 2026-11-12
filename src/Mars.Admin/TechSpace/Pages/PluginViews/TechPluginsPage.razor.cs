using Mars.Admin.Framework.Interfaces;
using Mars.Admin.SiteSpace.Pages.PluginViews;
using Mars.Plugin.Contracts.Catalog;
using Mars.Plugin.Contracts.Plugins;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.TechSpace.Pages.PluginViews;

public partial class TechPluginsPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IMessageService messageService { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;

    sealed record FilterDef(string Id, string Label, bool Available = true);

    static readonly FilterDef[] Filters =
    [
        new("all", "All"),
        new("enabled", "Enabled"),
        new("disabled", "Disabled"),
        new("backend", "Backend", Available: false),
        new("frontend", "Frontend", Available: false),
        new("updates", "Updates"),
    ];

    string _filter = "all";
    string _query = "";

    List<PluginInfoResponse>? plugins;
    bool loadFailed;
    Dictionary<string, CatalogPluginDto> catalog = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> busy = new(StringComparer.OrdinalIgnoreCase);

    // иконки отключённых (не загруженных) плагинов не раздаются сервером — буква вместо битой картинки
    readonly HashSet<string> iconFailed = new(StringComparer.OrdinalIgnoreCase);

    // SetEnabled пишет отложенное состояние в реестр, а List отдаёт runtime-состояние
    // до рестарта — показываем цель переключения, пока сервер её не применил
    readonly Dictionary<string, bool> pendingEnabled = new(StringComparer.OrdinalIgnoreCase);

    bool DisplayEnabled(PluginInfoResponse plugin)
        => pendingEnabled.TryGetValue(plugin.PackageId, out var target) ? target : plugin.Enabled;

    bool HasPendingRestart(PluginInfoResponse plugin) => pendingEnabled.ContainsKey(plugin.PackageId);

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var result = await client.Plugin.List(new ListPluginQueryRequest { Take = 1000 });
            plugins = [.. result.Items.OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase)];
        }
        catch
        {
            loadFailed = true;
            return;
        }

        // обогащение из каталога не блокирует показ списка: внешний сервис может быть недоступен
        _ = LoadCatalogAsync();
    }

    async Task LoadCatalogAsync()
    {
        try
        {
            var status = await client.Plugin.MarketplaceStatus();
            if (!status.Enabled) return;

            var response = await client.Plugin.MarketplaceSearch(new MarketplaceSearchRequest { Take = 200 });
            catalog = response.Items
                .GroupBy(p => p.PackageId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            StateHasChanged();
        }
        catch
        {
            // каталог недоступен — карточки работают без author/downloads/updates
        }
    }

    async Task ReloadAsync()
    {
        try
        {
            var result = await client.Plugin.List(new ListPluginQueryRequest { Take = 1000 });
            plugins = [.. result.Items.OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase)];
            foreach (var plugin in plugins)
            {
                if (pendingEnabled.TryGetValue(plugin.PackageId, out var target) && plugin.Enabled == target)
                    pendingEnabled.Remove(plugin.PackageId);
            }
        }
        catch (Exception ex)
        {
            _ = messageService.Error(ex.Message);
        }
    }

    IEnumerable<PluginInfoResponse> Filtered()
    {
        var text = _query.Trim();
        return plugins!.Where(plugin =>
            (_filter switch
            {
                "enabled" => DisplayEnabled(plugin),
                "disabled" => !DisplayEnabled(plugin),
                "updates" => HasUpdate(plugin),
                _ => true,
            })
            && (text.Length == 0
                || plugin.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
                || plugin.PackageId.Contains(text, StringComparison.OrdinalIgnoreCase)
                || (plugin.Description?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)));
    }

    bool ActionsDisabled(PluginInfoResponse plugin)
        => plugin.Locked || plugin.PendingDelete || busy.Contains(plugin.PackageId);

    async Task ToggleEnabled(PluginInfoResponse plugin)
    {
        if (!busy.Add(plugin.PackageId)) return;
        var target = !DisplayEnabled(plugin);
        try
        {
            await client.Plugin.SetEnabled(plugin.PackageId, target);
            pendingEnabled[plugin.PackageId] = target;
            _ = messageService.Success($"Плагин '{plugin.PackageId}' {(target ? "включён" : "отключён")}. Изменения применятся после рестарта.");
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _ = messageService.Error(ex.Message);
        }
        finally
        {
            busy.Remove(plugin.PackageId);
        }
    }

    async Task Update(PluginInfoResponse plugin)
    {
        if (!busy.Add(plugin.PackageId)) return;
        try
        {
            var result = await client.Plugin.InstallFromNuget(plugin.PackageId);
            _ = messageService.Success(result.Message);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _ = messageService.Error(ex.Message);
        }
        finally
        {
            busy.Remove(plugin.PackageId);
        }
    }

    async Task Delete(PluginInfoResponse plugin)
    {
        if (!await dialogService.MarsDeleteConfirmation($"Удалить плагин «{plugin.Title}»? Он будет удалён при следующем рестарте сервера."))
            return;

        if (!busy.Add(plugin.PackageId)) return;
        try
        {
            await client.Plugin.Uninstall(plugin.PackageId);
            _ = messageService.Success($"Плагин '{plugin.PackageId}' удалён. Изменения применятся после рестарта.");
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _ = messageService.Error(ex.Message);
        }
        finally
        {
            busy.Remove(plugin.PackageId);
        }
    }

    async Task OnClickUploadFromZipFile()
    {
        var result = await ZipUploadDialog.ShowAsync(dialogService);
        if (result is not null) await ReloadAsync();
    }

    async Task OnClickInstallFromNuget()
    {
        var result = await NugetInstallDialog.ShowAsync(dialogService);
        if (result is not null) await ReloadAsync();
    }

    CatalogPluginDto? CatalogInfo(PluginInfoResponse plugin)
        => catalog.TryGetValue(plugin.PackageId, out var info) ? info : null;

    string AuthorLine(PluginInfoResponse plugin)
        => CatalogInfo(plugin) is { } info
            ? $"{info.AuthorName ?? plugin.PackageId} · {Downloads(info.TotalDownloads)} installs"
            : plugin.PackageId;

    string? LatestVersion(PluginInfoResponse plugin) => CatalogInfo(plugin)?.LatestVersion;

    bool HasUpdate(PluginInfoResponse plugin)
        => LatestVersion(plugin) is { } latest && IsNewer(plugin.Version, latest);

    static bool IsNewer(string? installed, string? latest)
    {
        if (string.IsNullOrEmpty(installed) || string.IsNullOrEmpty(latest)) return false;
        if (Version.TryParse(Strip(installed), out var current) && Version.TryParse(Strip(latest), out var available))
            return available > current;
        return !string.Equals(installed, latest, StringComparison.OrdinalIgnoreCase);
    }

    static string Strip(string version) => version.Split('+')[0].Split('-')[0];

    static string SourceLabel(PluginInfoResponse plugin) => plugin.Source switch
    {
        PluginSource.NuGet => "NuGet",
        PluginSource.Zip => "Zip",
        PluginSource.Config => "Config",
        _ => "Plugin",
    };

    static char Initial(PluginInfoResponse plugin)
        => string.IsNullOrEmpty(plugin.Title) ? '?' : plugin.Title[0];

    static string InstalledLabel(PluginInfoResponse plugin)
        => plugin.InstalledAt == default ? "From config" : $"Installed {plugin.InstalledAt.ToString("d")}";

    static string Downloads(long count)
        => count >= 1000 ? (count / 1000.0).ToString("0.#") + "k" : count.ToString();
}
