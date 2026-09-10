using System.Collections.ObjectModel;
using Mars.Plugin.Contracts.Plugins;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mars.Admin.Pages.PluginViews;

public partial class PluginsListPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IJSRuntime jSRuntime { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;
    [Inject] NavigationManager navigationManager { get; set; } = default!;

    FluentDataGrid<PluginInfoResponse> table = default!;
    string _searchText = "";
    ListDataResult<PluginInfoResponse> data = ListDataResult<PluginInfoResponse>.Empty();
    GridItemsProvider<PluginInfoResponse> dataProvider = default!;

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<PluginInfoResponse>(
            async req =>
            {
                _ = req.SortByAscending;
                _ = req.SortByColumn;

                var sortColumn = req.GetSortByProperties().Count == 0 ? "Title" : req.GetSortByProperties().First().PropertyName;

                var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                data = await client.Plugin.List(new()
                {
                    //Page = pagination.CurrentPageIndex + 1,
                    //PageSize = pagination.ItemsPerPage,
                    Skip = req.StartIndex,
                    Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
                });

                var collection = new Collection<PluginInfoResponse>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    void OnRowClick(FluentDataGridRow<PluginInfoResponse> row)
    {

        if (row.Item is null) return;

        //DialogParameters parameters = new()
        //{
        //    Title = row.Item.Title,
        //    //PrimaryActionEnabled = false,
        //    //PrimaryAction = "Yes",
        //    SecondaryAction = null,
        //    //Width = "500px",
        //    //TrapFocus = _trapFocus,
        //    //Modal = _modal,
        //    PreventScroll = true
        //};

        //var detail = await client.Plugin.Get(row.Item.Id);

        //if (detail is not null)
        //{
        //    IDialogReference dialog = await dialogService.ShowDialogAsync<ViewFeedbackDialog>(detail, parameters);
        //    DialogResult? result = await dialog.Result;
        //}
        //else
        //{
        //    _ = _messageService.Error("element not found");
        //}

    }

    public async Task Delete(PluginInfoResponse context)
    {
        try
        {
            await client.Plugin.Uninstall(context.PackageId);
            _ = _messageService.Success($"Плагин '{context.PackageId}' удалён. Изменения применятся после рестарта.");
            _ = table.RefreshDataAsync();
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
        }
    }

    public async Task ToggleEnabled(PluginInfoResponse context)
    {
        try
        {
            await client.Plugin.SetEnabled(context.PackageId, !context.Enabled);
            _ = _messageService.Success($"Плагин '{context.PackageId}' {(context.Enabled ? "отключён" : "включён")}. Изменения применятся после рестарта.");
            _ = table.RefreshDataAsync();
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
        }
    }

    public async Task Update(PluginInfoResponse context)
    {
        try
        {
            var result = await client.Plugin.InstallFromNuget(context.PackageId);
            _ = _messageService.Success($"{result.Message}");
            _ = table.RefreshDataAsync();
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
        }
    }

    public async Task OnClickUploadFromZipFile()
    {
        var result = await ZipUploadDialog.ShowAsync(dialogService);
        _ = table.RefreshDataAsync();
    }

    public async Task OnClickInstallFromNuget()
    {
        var result = await NugetInstallDialog.ShowAsync(dialogService);
        _ = table.RefreshDataAsync();
    }

    public void OnClickMarketplace() => navigationManager.NavigateTo("/marketplace");
}
