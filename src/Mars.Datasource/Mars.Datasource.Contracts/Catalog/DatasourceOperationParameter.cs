namespace Mars.Datasource.Contracts.Catalog;

/// <summary>Параметр операции каталога: из OpenAPI, из индекса REST API (WordPress <c>args</c>) или заданный вручную.</summary>
public class DatasourceOperationParameter
{
    public string Name { get; set; } = "";

    /// <summary>query | path | header | body</summary>
    public string In { get; set; } = DatasourceParameterIn.Query;

    public string Type { get; set; } = "string";

    public bool Required { get; set; }

    public string? Default { get; set; }

    public List<string>? Enum { get; set; }

    public string? Description { get; set; }
}

public static class DatasourceParameterIn
{
    public const string Query = "query";
    public const string Path = "path";
    public const string Header = "header";
    public const string Body = "body";
}
