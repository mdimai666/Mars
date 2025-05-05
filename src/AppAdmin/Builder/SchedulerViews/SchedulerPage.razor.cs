using AppFront.Main.Extensions;
using Mars.Shared.Contracts.Schedulers;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppAdmin.Builder.SchedulerViews;

public partial class SchedulerPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IMessageService messageService { get; set; } = default!;

    PaginationState pagination = new PaginationState { ItemsPerPage = 20 };

    PagingResult<SchedulerJobResponse>? res;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _ = Load();
    }


    async Task Load()
    {
        var query = new TableSchedulerJobQueryRequest { Page = pagination.CurrentPageIndex + 1, PageSize = pagination.ItemsPerPage };
        res = await client.Scheduler.JobListTable(query);
        await pagination.SetTotalItemCountAsync(res.TotalCount ?? res.ItemsCount);
        StateHasChanged();
    }

    private async Task GoToPageAsync(int pageIndex)
    {
        await pagination.SetCurrentPageIndexAsync(pageIndex);
        _ = Load();
    }

    private Appearance PageButtonAppearance(int pageIndex)
        => pagination.CurrentPageIndex == pageIndex ? Appearance.Accent : Appearance.Neutral;

    private string? AriaCurrentValue(int pageIndex)
        => pagination.CurrentPageIndex == pageIndex ? "page" : null;

    private string AriaLabel(int pageIndex)
        => $"Go to page {pageIndex}";

    private async void PlayAll()
    {
        await client.Scheduler.ResumeAll().SmartSuccess();
        RefreshClick();
    }

    private async void PauseAll()
    {
        await client.Scheduler.PauseAll().SmartSuccess();
        RefreshClick();
    }

    private void RefreshClick()
    {
        _ = Load();
    }

    private void DeleteItem(SchedulerJobResponse item)
    {

    }

    private void EditItem(SchedulerJobResponse item)
    {

    }

    private async void PlayItem(SchedulerJobResponse item)
    {
        await client.Scheduler.ResumeJob(item.Name, item.Group!).SmartSuccess();
        RefreshClick();
    }

    private async void PauseItem(SchedulerJobResponse item)
    {
        await client.Scheduler.PauseJob(item.Name, item.Group).SmartSuccess();
        RefreshClick();
    }
    private async void InjectItem(SchedulerJobResponse item)
    {
        await client.Scheduler.InjectJob(item.Name, item.Group).SmartSuccess();
        RefreshClick();
    }

}
