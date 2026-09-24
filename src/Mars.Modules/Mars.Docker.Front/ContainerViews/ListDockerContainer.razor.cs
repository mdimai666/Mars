using Mars.Admin.Framework.Components;
using Mars.Admin.Framework.Extensions;
using Mars.Admin.Framework.Interfaces;
using Mars.Contracts.Resources;
using Mars.Docker.Contracts;
using Mars.Docker.Front.Services;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using IMessageService = Mars.Admin.Framework.Interfaces.IMessageService;

namespace Mars.Docker.Front.ContainerViews;

public class DockerGridRow
{
    public string? GroupName { get; init; }
    public List<ContainerListResponse1> Containers { get; init; } = [];
    public ContainerListResponse1? Container { get; init; }
    public string? ParentGroup { get; init; }
}

public class DockerGridItem : HierarchicalGridItem<DockerGridRow, DockerGridItem>
{
}

public partial class ListDockerContainer
{
    public const string ComposeProjectLabel = "com.docker.compose.project";

    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;
    [Inject] NavigationManager navigation { get; set; } = default!;
    [Inject] IMessageService messageService { get; set; } = default!;

    const string UrlEditPage = "/dev/builder/docker/ID";
    const int OnceCaptureTimeoutSeconds = 60;

    string _searchText = "";
    bool _runningOnly;
    bool IsBusy { get; set; }
    List<DockerGridItem> _items = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        IsBusy = true;
        StateHasChanged();

        var data = await client.Docker().ListContainers(new ListContainerRequest
        {
            Take = 1000,
            Search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
        });

        var collapsedState = _items
            .Where(i => i.Item.GroupName is not null)
            .ToDictionary(i => i.Item.GroupName!, i => i.IsCollapsed);

        var filtered = data.Items.Where(c => !_runningOnly || c.State == "running").ToList();

        var groups = filtered
            .Where(c => c.Labels.TryGetValue(ComposeProjectLabel, out var project) && !string.IsNullOrEmpty(project))
            .GroupBy(c => c.Labels[ComposeProjectLabel])
            .Select(g => (Name: g.Key, Items: g.OrderByDescending(c => c.StartedAt ?? c.Created).ToList()))
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var standalone = filtered
            .Where(c => !c.Labels.TryGetValue(ComposeProjectLabel, out var project) || string.IsNullOrEmpty(project))
            .OrderByDescending(c => c.StartedAt ?? c.Created)
            .ToList();

        _items = [];
        foreach (var group in groups)
        {
            var groupItem = new DockerGridItem
            {
                Item = new DockerGridRow { GroupName = group.Name, Containers = group.Items },
                IsCollapsed = collapsedState.TryGetValue(group.Name, out var wasCollapsed) ? wasCollapsed : true,
            };
            _items.Add(groupItem);

            foreach (var container in group.Items)
            {
                var childItem = new DockerGridItem
                {
                    Item = new DockerGridRow { Container = container, ParentGroup = group.Name },
                    Depth = 1,
                    IsHidden = groupItem.IsCollapsed,
                };
                groupItem.Children.Add(childItem);
                _items.Add(childItem);
            }
        }

        foreach (var container in standalone)
        {
            _items.Add(new DockerGridItem { Item = new DockerGridRow { Container = container } });
        }

        IsBusy = false;
        StateHasChanged();
    }

    void HandleSearchInput()
    {
        _ = LoadAsync();
    }

    public Task Refresh() => LoadAsync();

    static bool GroupIsRunning(DockerGridRow row) => row.Containers.Any(c => c.State == "running");

    public static string DisplayName(DockerGridRow row)
    {
        var name = (row.Container?.Names.FirstOrDefault() ?? row.Container?.ID ?? "").TrimStart('/');
        if (row.ParentGroup is { Length: > 0 } group && name.StartsWith(group, StringComparison.OrdinalIgnoreCase))
        {
            name = name[group.Length..].TrimStart('-', '_');
        }

        return name;
    }

    async Task ToggleGroup(DockerGridItem groupItem)
    {
        var stopping = GroupIsRunning(groupItem.Item);
        try
        {
            await Task.WhenAll(groupItem.Item.Containers
                .Where(c => stopping ? c.State == "running" : c.State != "running")
                .Select(c => stopping
                    ? client.Docker().StopContainer(c.ID)
                    : client.Docker().StartContainer(c.ID)));
            _ = messageService.Success(AppRes.CompletedSuccessfully);
        }
        catch (Exception ex)
        {
            _ = messageService.Error(ex.Message);
        }

        await LoadAsync();
    }

    async Task ToggleContainer(ContainerListResponse1 container)
    {
        var task = container.State == "running"
            ? client.Docker().StopContainer(container.ID)
            : client.Docker().StartContainer(container.ID);

        if (await task.SmartSuccess())
        {
            await LoadAsync();
        }
    }

    async Task ShowCreateDialog()
    {
        var dialog = await dialogService.ShowDialogAsync<CreateContainerDialog>("", new DialogParameters
        {
            Title = "Create container",
            Modal = true,
        });
        var result = await dialog.Result;
        if (result.Cancelled)
        {
            return;
        }

        if (result.Data is not CreateContainerDialogResult created)
        {
            return;
        }

        IsBusy = true;
        StateHasChanged();
        try
        {
            // auto-remove удаляет контейнер вместе с логами сразу после выхода —
            // создаём без него, вывод перехватывает сервер и удаляет контейнер сам
            var removeAfterExit = created.StartAfterCreate && created.Request.AutoRemove;
            var request = removeAfterExit ? created.Request with { AutoRemove = false } : created.Request;

            var response = await client.Docker().CreateContainer(request);
            if (created.StartAfterCreate)
            {
                if (removeAfterExit)
                {
                    await client.Docker().StartAndCapture(response.ID, OnceCaptureTimeoutSeconds, removeAfterExit: true);
                }
                else
                {
                    await client.Docker().StartContainer(response.ID);
                }

                navigation.NavigateTo($"{UrlEditPage}/{response.ID}?once=1");
            }
            else
            {
                navigation.NavigateTo($"{UrlEditPage}/{response.ID}");
            }
        }
        catch (Exception ex)
        {
            _ = messageService.Error(ex.Message);
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
            StateHasChanged();
        }
    }

    async Task DeleteContainer(ContainerListResponse1 container)
    {
        var name = container.Names.FirstOrDefault() ?? container.ID;
        var dialog = await dialogService.ShowDialogAsync<DeleteConfirmationDialog>(
            (MarkupString)$"Delete container <b>{name}</b>?",
            new DialogParameters { Title = "Delete container", Modal = true });
        var result = await dialog.Result;
        if (result.Cancelled)
        {
            return;
        }

        if (await client.Docker().DeleteContainer(container.ID).SmartDelete())
        {
            await LoadAsync();
        }
    }

    public async Task RestartContainer(ContainerListResponse1 container)
    {
        await client.Docker().RestartContainer(container.ID).SmartSuccess();
        await LoadAsync();
    }
}
