using Mars.Contracts.Common;
using Mars.Core.Extensions;
using Mars.Media.Contracts.Files;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.SiteSpace.Pages.MediaViews;

public partial class ManageMediaPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    IReadOnlyDictionary<string, KpiResult> _kpi = new Dictionary<string, KpiResult>();

    protected override async Task OnInitializedAsync()
    {
        await LoadKpi();
    }

    async Task LoadKpi()
    {
        _kpi = await client.Kpi.Get([MediaKpiKeys.Files, MediaKpiKeys.Storage]);
    }

    string KpiLabel(string key)
        => _kpi.TryGetValue(key, out var r) && !string.IsNullOrEmpty(r.Label) ? L[r.Label].Value : key;

    string KpiValue(string key)
        => _kpi.TryGetValue(key, out var r) ? r.Value.ToString("N0") : "—";

    string StorageValue()
        => _kpi.TryGetValue(MediaKpiKeys.Storage, out var r) ? ((long)r.Value).ToHumanizedSize() : "—";
}
