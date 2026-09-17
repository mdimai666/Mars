using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Front.Services;
using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front;

/// <summary>
/// Точка входа страницы запросов: по типу источника выбирает мир — SQL-страницу или страницу объектов.
/// Каталог грузит уже сама страница, здесь только выбор (источники читаются лёгким списком).
/// </summary>
public partial class QueryPage : ComponentBase
{
    [Inject] IDatasourceServiceClient service { get; set; } = default!;

    [Parameter]
    public string DataSourceConfigSlug { get; set; } = DatasourceConfig.DefaultSlug;

    string? slugLoaded;
    bool loading = true;
    string? error;

    bool IsSql => string.Equals(kind, DatasourceKind.Sql, StringComparison.OrdinalIgnoreCase);

    string kind = DatasourceKind.Sql;

    protected override async Task OnParametersSetAsync()
    {
        if (slugLoaded == DataSourceConfigSlug) return;

        slugLoaded = DataSourceConfigSlug;

        await LoadAsync();
    }

    async Task LoadAsync()
    {
        loading = true;
        error = null;
        StateHasChanged();

        try
        {
            var sources = await service.ListSelectDatasource();
            var source = sources.FirstOrDefault(item => string.Equals(item.Slug, DataSourceConfigSlug, StringComparison.OrdinalIgnoreCase));

            if (source is null)
            {
                error = $"Источник «{DataSourceConfigSlug}» не найден";
                return;
            }

            kind = string.IsNullOrWhiteSpace(source.Kind) ? DatasourceKind.Sql : source.Kind;
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            loading = false;
            StateHasChanged();
        }
    }
}
