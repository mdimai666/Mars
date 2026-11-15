using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Front.Services;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.TechSpace.Pages.DataSourceViews;

/// <summary>
/// Универсальная страница источников: hero с переключателем и обслуживанием, ниже — рабочая
/// область движка (<see cref="Mars.Datasource.Front.QueryPage"/> сам выбирает мир по типу источника).
/// Тип в маршруте не фигурирует: один источник — один slug, мир определяется его Kind.
/// </summary>
public partial class DatasourcePage
{
    [Inject] IDatasourceServiceClient service { get; set; } = default!;
    [Inject] NavigationManager nav { get; set; } = default!;

    [Parameter]
    public string? Slug { get; set; }

    IReadOnlyCollection<SelectDatasourceDto> sources = [];
    bool loading = true;
    string? loadError;
    bool maintenanceOpen;

    /// <summary>Slug из маршрута; без него — default, а если default не настроен — первый источник.</summary>
    string EffectiveSlug
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Slug)) return Slug;

            return sources.Any(item => item.Slug == DatasourceConfig.DefaultSlug)
                ? DatasourceConfig.DefaultSlug
                : sources.FirstOrDefault()?.Slug ?? DatasourceConfig.DefaultSlug;
        }
    }

    protected override Task OnInitializedAsync() => LoadAsync();

    async Task LoadAsync()
    {
        loading = true;
        loadError = null;

        try
        {
            sources = await service.ListSelectDatasource();
        }
        catch (Exception ex)
        {
            loadError = ex.Message;
        }
        finally
        {
            loading = false;
        }
    }

    void OnSourceChanged(ChangeEventArgs e)
    {
        var slug = e.Value?.ToString();

        if (!string.IsNullOrEmpty(slug) && slug != EffectiveSlug)
        {
            nav.NavigateTo($"tech/datasource/{Uri.EscapeDataString(slug)}");
        }
    }

    static string KindLabel(string kind) => kind switch
    {
        DatasourceKind.Sql => "SQL",
        DatasourceKind.Rest => "REST",
        DatasourceKind.File => "File",
        _ => kind,
    };
}