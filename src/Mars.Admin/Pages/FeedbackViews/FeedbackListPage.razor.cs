using Mars.Admin.Shared.Cloudy;
using Mars.Cms.Contracts.Feedbacks;
using Mars.Contracts.Common;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.FeedbackViews;

public partial class FeedbackListPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] AdminJs _appjs { get; set; } = default!;

    FluentDataGrid<FeedbackSummaryResponse> _grid = default!;
    GridItemsProvider<FeedbackSummaryResponse> _dataProvider = default!;

    string _searchText = "";
    int? _total;
    IReadOnlyDictionary<string, KpiResult> _kpi = new Dictionary<string, KpiResult>();

    protected override async Task OnInitializedAsync()
    {
        _dataProvider = CloudyGridProvider.Create<FeedbackSummaryResponse>(LoadFeedbacks);
        await LoadKpi();
    }

    async Task<ListDataResult<FeedbackSummaryResponse>> LoadFeedbacks(int skip, int take, string? sort)
    {
        var data = await client.Feedback.List(new()
        {
            Skip = skip,
            Take = take,
            Sort = sort ?? $"-{nameof(FeedbackSummaryResponse.CreatedAt)}",
            Search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
        });

        _total = data.TotalCount ?? data.Items.Count;
        StateHasChanged();
        return data;
    }

    async Task LoadKpi()
    {
        _kpi = await client.Kpi.Get([FeedbackKpiKeys.Total, FeedbackKpiKeys.NewThisWeek]);
    }

    string KpiLabel(string key)
        => _kpi.TryGetValue(key, out var r) && !string.IsNullOrEmpty(r.Label) ? L[r.Label].Value : key;

    string KpiValue(string key)
        => _kpi.TryGetValue(key, out var r) ? r.Value.ToString("N0") : "—";

    void RefreshGrid() => _grid?.RefreshDataAsync();

    void OnSearchChanged(string text)
    {
        _searchText = text;
        RefreshGrid();
    }

    async Task OnRowClick(FluentDataGridRow<FeedbackSummaryResponse> row)
    {
        if (row.Item is null) return;

        var detail = await client.Feedback.Get(row.Item.Id);
        if (detail is null)
        {
            _ = _messageService.Error("element not found");
            return;
        }

        await _dialogService.ShowDialogAsync<ViewFeedbackDialog>(new DialogOptions
        {
            Modal = true,
            Header = { CloseAction = { Visible = true } },
            Parameters = { ["Content"] = detail },
        });
    }

    public async Task Delete(Guid id)
    {
        await client.Feedback.Delete(id).SmartDelete();
        RefreshGrid();
        await LoadKpi();
    }

    async Task DownloadExcel()
    {
        string url = Q.ServerUrlJoin("api/Feedback/DownloadExcel");
        await _appjs.DownloadFileFromUrl(url);
    }

    // имена значений FeedbackType (Mars.Cms.Abstractions); GetHashCode у string рандомизирован на процесс — только фиксированный маппинг
    static int TintFor(string type) => type switch
    {
        "InfoMessage" => 0,
        "BugReport" => 3,
        "Question" => 1,
        _ => StableTint(type),
    };

    static int StableTint(string s)
    {
        var hash = 0;
        foreach (var c in s) hash = unchecked(hash * 31 + c);
        return (hash & 0x7FFFFFFF) % 5;
    }
}
