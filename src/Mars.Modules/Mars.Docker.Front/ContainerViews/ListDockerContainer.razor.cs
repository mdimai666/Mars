using System.Collections.ObjectModel;
using AppFront.Main.Extensions;
using Mars.Docker.Contracts;
using Mars.Docker.Front.Services;
using Mars.Shared.Common;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Docker.Front.ContainerViews;

public partial class ListDockerContainer
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;

    string urlEditPage = "/dev/builder/docker/ID";

    //table
    FluentDataGrid<ContainerListResponse1> table = default!;
    string _searchText = "";
    ListDataResult<ContainerListResponse1> data = ListDataResult<ContainerListResponse1>.Empty();
    GridItemsProvider<ContainerListResponse1> dataProvider = default!;

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<ContainerListResponse1>(
            async req =>
            {
                _ = req.SortByAscending;
                _ = req.SortByColumn;

                var sortColumn = req.GetSortByProperties().Count == 0 ? nameof(ContainerListResponse1.Created) : req.GetSortByProperties().First().PropertyName;

                var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                data = await client.Docker().ListContainers(new()
                {
                    //Page = pagination.CurrentPageIndex + 1,
                    //PageSize = pagination.ItemsPerPage,
                    Skip = req.StartIndex,
                    Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
                });

                var collection = new Collection<ContainerListResponse1>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    public Task Delete(string id)
    {
        throw new NotImplementedException();
        //await client.Docker().Delete(id).SmartDelete();
        _ = table.RefreshDataAsync();
    }

    public async Task StartContainer(ContainerListResponse1 container)
    {
        await client.Docker().StartContainer(container.ID).SmartSuccess();
        HandleSearchInput();
    }

    public async Task StopContainer(ContainerListResponse1 container)
    {
        await client.Docker().StopContainer(container.ID).SmartSuccess();
        HandleSearchInput();
    }
}
