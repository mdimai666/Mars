using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front.Workspaces.Shared;

/// <summary>
/// Форма параметров операции: значения живут во вкладке (они же переменные документа запросов),
/// поэтому компонент только читает и сообщает о правке.
/// </summary>
public partial class OperationParametersPanel : ComponentBase
{
    [Parameter] public DatasourceCatalogObject? Operation { get; set; }
    [Parameter] public Func<string, string?> GetValue { get; set; } = _ => null;
    [Parameter] public EventCallback<(string Name, string? Value)> ValueChanged { get; set; }
}
