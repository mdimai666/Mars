using Mars.Admin.Framework.Components;
using Mars.Admin.Framework.Extensions;
using Mars.Docker.Contracts;
using Mars.Docker.Front.Services;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Docker.Front.ImageViews;

public partial class ListDockerImage
{
    public const string NoneRepo = "<none>";

    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;

    string _searchText = "";
    bool IsBusy { get; set; }
    List<ImageGroup> Groups { get; set; } = [];
    readonly HashSet<string> _collapsedGroups = [];

    public record ImageTagEntry(string Repo, string Tag, string FullName, ImageSummaryResponse1 Image);
    public record ImageGroup(string Name, List<ImageTagEntry> Items);

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        IsBusy = true;
        StateHasChanged();

        var data = await client.Docker().ListImages(new ListImageRequest
        {
            Take = 1000,
            Search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
        });

        var entries = data.Items.SelectMany(image => image.RepoTags.Count > 0
            ? image.RepoTags.Select(tag => ToEntry(tag, image))
            : [new ImageTagEntry(NoneRepo, ShortId(image.ID), image.ID, image)]);

        Groups = entries
            .GroupBy(e => e.Repo)
            .Select(g => new ImageGroup(g.Key, g.OrderBy(e => e.Tag, StringComparer.OrdinalIgnoreCase).ToList()))
            .OrderBy(g => g.Name == NoneRepo ? 1 : 0)
            .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        IsBusy = false;
        StateHasChanged();
    }

    static ImageTagEntry ToEntry(string repoTag, ImageSummaryResponse1 image)
    {
        var (repo, tag) = SplitRepoTag(repoTag);
        return new ImageTagEntry(repo, tag, repoTag, image);
    }

    /// <summary>Отделяет тег: последний <c>:</c> после последнего <c>/</c> (registry может содержать порт).</summary>
    internal static (string Repo, string Tag) SplitRepoTag(string repoTag)
    {
        var lastSlash = repoTag.LastIndexOf('/');
        var colon = repoTag.IndexOf(':', lastSlash + 1);
        return colon < 0 ? (repoTag, "latest") : (repoTag[..colon], repoTag[(colon + 1)..]);
    }

    internal static string ShortId(string id)
    {
        var raw = id.StartsWith("sha256:") ? id["sha256:".Length..] : id;
        return raw.Length > 12 ? raw[..12] : raw;
    }

    internal static string HumanSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{size:F1} {units[unit]}";
    }

    void HandleSearchInput()
    {
        _ = LoadAsync();
    }

    public Task Refresh() => LoadAsync();

    bool IsCollapsed(string groupName) => _collapsedGroups.Contains(groupName);

    string ChevronClass(string groupName) => IsCollapsed(groupName) ? "docker-group-chevron collapsed" : "docker-group-chevron";

    void ToggleGroup(string groupName)
    {
        if (!_collapsedGroups.Remove(groupName))
        {
            _collapsedGroups.Add(groupName);
        }
    }

    async Task ShowPullDialog()
    {
        var dialog = await dialogService.ShowDialogAsync<PullImageDialog>("", new DialogParameters
        {
            Title = "Pull image",
            Modal = true,
        });
        var result = await dialog.Result;
        if (result.Cancelled)
        {
            return;
        }

        if (result.Data is true)
        {
            await LoadAsync();
        }
    }

    async Task DeleteImage(ImageTagEntry entry)
    {
        var dialog = await dialogService.ShowDialogAsync<DeleteConfirmationDialog>(
            (MarkupString)$"Delete image <b>{entry.FullName}</b>?",
            new DialogParameters { Title = "Delete image", Modal = true });
        var result = await dialog.Result;
        if (result.Cancelled)
        {
            return;
        }

        var target = entry.Repo == NoneRepo ? entry.Image.ID : entry.FullName;
        if (await client.Docker().DeleteImage(target).SmartDelete())
        {
            await LoadAsync();
        }
    }
}
