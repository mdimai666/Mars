using AppFront.Shared.Interfaces;
using Mars.Core.Extensions;
using Mars.Datasource.Core.Dto;
using Mars.Shared.Common;
using Mars.WebApiClient.Interfaces;
using MarsEditors;
using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front;

public partial class DatabaseQueryWorkspace
{
    [Inject] IDatasourceServiceClient service { get; set; } = default!;
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IMessageService _messageService { get; set; } = default!;

    bool Busy;

    string _dataSourceConfigSlug = "default";

    [Parameter]
    public string DataSourceConfigSlug
    {
        get => _dataSourceConfigSlug ?? "default";
        set
        {
            if (_dataSourceConfigSlug != value)
            {
                _dataSourceConfigSlug = value;
                Load();
            }
        }
    }

    QDatabaseStructureResponse database = null!;

    QTableResponse? selTable = null;

    string[][]? raw => res?.Data;

    UserActionResult<string[][]>? res = null;

    bool loadingQuery = false;

    MarsCodeEditor? _editor = default!;

    IReadOnlyCollection<SelectDatasourceDto> listDatasources = new List<SelectDatasourceDto>();

    string thisurl = "";

    protected override void OnInitialized()
    {
        Console.WriteLine(">>>I: " + DataSourceConfigSlug);
        base.OnInitialized();
        thisurl = new Uri(nav.Uri).LocalPath;
        Load();
    }

    async void Load()
    {
        Console.WriteLine(">>>L: " + DataSourceConfigSlug);

        Busy = true;
        StateHasChanged();

        database = await service.DatabaseStructure(DataSourceConfigSlug);

        _ = Task.Run(async () =>
        {
            listDatasources = await service.ListSelectDatasource();
            StateHasChanged();
        });

        Busy = false;
        StateHasChanged();
    }

    async void OnClickTable(QTableResponse table)
    {
        selTable = table;

        await Task.Delay(100);

        _editor?.SetValue(GetSelectRowsQuery(table.TableName));
    }

    string GetSelectRowsQuery(string tableName)
    {
        var d = listDatasources.FirstOrDefault(s => s.Slug == DataSourceConfigSlug);
        var q = d?.EscapeQuotationMark ?? '"';
        if (d is not null)
        {
            if (d.Driver == "mssql") return $"SELECT TOP 20 * FROM {q}{tableName}{q}\n";
        }
        return $"SELECT * FROM {q}{tableName}{q}\nLIMIT 20";
    }

    async void ClickQuery()
    {
        if (selTable is not null)
        {
            loadingQuery = true;
            StateHasChanged();

            _ = WaitHelper.WaitForNotNull(() => _editor, 2000);

            string sql = await _editor!.GetValue();

            if (string.IsNullOrEmpty(sql))
            {
                _ = _messageService.Error("SQL query is empty!");
                loadingQuery = false;
                StateHasChanged();
                return;
            }

            //res = await service.SqlQuery($"SELECT * FROM \"{selTable.TableName}\"");
            res = await service.SqlQuery(DataSourceConfigSlug, sql);

            loadingQuery = false;
            StateHasChanged();
        }
    }

    async void ShowRecords(QTableResponse table)
    {
        selTable = table;
        await Task.Delay(100);

        ClickQuery();
    }

    //=================
    //private MonacoEditor? _editor { get; set; }

    //private StandaloneEditorConstructionOptions EditorConstructionOptions(MonacoEditor editor)
    //{

    //    return new StandaloneEditorConstructionOptions
    //        {

    //            AutomaticLayout = true,
    //            Language = "sql",
    //            Value = "SELECT * from posts",


    //        };
    //}

}
