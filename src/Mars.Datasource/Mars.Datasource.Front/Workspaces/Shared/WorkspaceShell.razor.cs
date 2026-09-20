using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front.Workspaces.Shared;

/// <summary>
/// Каркас рабочей области источника: шапка, дерево, вкладки, тулбар, редактор и результат.
/// Одинаков для всех типов источника — страница мира (SQL, объекты, дальше GraphQL) передаёт сюда
/// свои кнопки и панель над редактором фрагментами, а не копирует разметку.
/// </summary>
public partial class WorkspaceShell : ComponentBase
{
    [Parameter] public bool Busy { get; set; }

    [Parameter] public string? Error { get; set; }

    [Parameter] public string Slug { get; set; } = DatasourceConfig.DefaultSlug;

    [Parameter] public IReadOnlyCollection<SelectDatasourceDto> Sources { get; set; } = [];

    [Parameter] public SelectDatasourceDto? Source { get; set; }

    [Parameter] public string SourceLabel { get; set; } = "";

    [Parameter] public DatasourceCatalog? Catalog { get; set; }

    [Parameter, EditorRequired] public WorkspaceTreeState Tree { get; set; } = default!;

    [Parameter, EditorRequired] public List<QueryTab> Tabs { get; set; } = default!;

    [Parameter] public QueryTab? ActiveTab { get; set; }

    [Parameter] public EventCallback OnRetry { get; set; }

    [Parameter] public EventCallback<CatalogEntry> OnOpenObject { get; set; }

    [Parameter] public EventCallback OnRefreshCatalog { get; set; }

    [Parameter] public EventCallback<QueryTab> OnSelectTab { get; set; }

    [Parameter] public EventCallback<QueryTab> OnCloseTab { get; set; }

    [Parameter] public EventCallback OnAddTab { get; set; }

    [Parameter] public EventCallback OnRun { get; set; }

    [Parameter] public EventCallback OnRunMore { get; set; }

    /// <summary>Подпись кнопки запуска: у документа запросов она другая («Выполнить блок»).</summary>
    [Parameter] public string RunLabel { get; set; } = "Выполнить";

    /// <summary>Что открыто справа в тулбаре: объект дерева или операция каталога.</summary>
    [Parameter] public string TrailingLabel { get; set; } = "";

    [Parameter] public string? TrailingTitle { get; set; }

    /// <summary>Подсказка под редактором, когда результата ещё нет: текст из профиля источника.</summary>
    [Parameter] public string Hint { get; set; } = "";

    [Parameter] public int ObjectCount { get; set; }

    /// <summary>Кнопки дерева помимо «обновить».</summary>
    [Parameter] public RenderFragment? TreeActions { get; set; }

    /// <summary>Кнопки тулбара после «выполнить».</summary>
    [Parameter] public RenderFragment? ToolbarActions { get; set; }

    /// <summary>Панель над редактором: структура объекта или форма параметров операции.</summary>
    [Parameter] public RenderFragment? AboveEditor { get; set; }

    /// <summary>Сам редактор: остаётся у страницы, ей принадлежат ссылка на Monaco и его готовность.</summary>
    [Parameter] public RenderFragment? Editor { get; set; }
}
