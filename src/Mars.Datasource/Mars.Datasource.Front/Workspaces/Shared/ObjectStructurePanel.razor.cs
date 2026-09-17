using Mars.Datasource.Contracts.Models;
using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front.Workspaces.Shared;

/// <summary>Структура открытого объекта: колонки с типами и пометкой первичного ключа.</summary>
public partial class ObjectStructurePanel : ComponentBase
{
    [Parameter] public DatasourceCatalogObject? Object { get; set; }
}
