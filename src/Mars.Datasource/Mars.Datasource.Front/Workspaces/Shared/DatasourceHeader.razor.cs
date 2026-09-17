using Mars.Datasource.Contracts.Models;
using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front.Workspaces.Shared;

/// <summary>Шапка страницы запросов: переключатель источников и сводка по открытому.</summary>
public partial class DatasourceHeader : ComponentBase
{
    [Inject] NavigationManager nav { get; set; } = default!;

    [Parameter, EditorRequired] public string Slug { get; set; } = "";
    [Parameter] public IReadOnlyCollection<SelectDatasourceDto> Sources { get; set; } = [];
    [Parameter] public string SourceName { get; set; } = "";
    [Parameter] public int ObjectCount { get; set; }
    [Parameter] public string SourceLabel { get; set; } = "";

    string ConfigUrl => $"{nav.BaseUri}datasource/config";
}
