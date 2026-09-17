using Mars.Datasource.Contracts.Models;
using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front.Workspaces.Shared;

/// <summary>
/// Панель дерева объектов источника: фильтр, группы (схемы или HTTP-методы) и список объектов.
/// Кнопки панели приходят содержимым — у SQL это «вьюха» и «обновить», у остальных «обновить» и «.http».
/// </summary>
public partial class ObjectsTree : ComponentBase
{
    [Parameter, EditorRequired] public IEnumerable<DatasourceCatalogGroup> Groups { get; set; } = [];
    [Parameter] public string Filter { get; set; } = "";
    [Parameter] public EventCallback<string> FilterChanged { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public bool HasMultipleGroups { get; set; }
    [Parameter] public Func<string, bool> IsExpanded { get; set; } = _ => true;
    [Parameter] public EventCallback<string> OnToggleGroup { get; set; }
    [Parameter] public Func<DatasourceCatalogObject, bool> IsActive { get; set; } = _ => false;
    [Parameter] public EventCallback<CatalogEntry> OnOpen { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
