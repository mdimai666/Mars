using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Contracts.Sql;

namespace Mars.Datasource.Abstractions.Sql;

/// <summary>
/// Профиль sql-источника: общий для всех движков, различаются подпись, строка подключения
/// и группа дерева по умолчанию (у MsSQL схема `dbo`, у остальных `public`).
/// </summary>
public static class SqlDatasourceProfile
{
    public static DatasourceKindProfile Create(IDatasourceDriverFactory driverFactory) => new()
    {
        Kind = DatasourceKind.Sql,
        Driver = driverFactory.Driver,
        KindLabel = "SQL — реляционная база",
        Label = driverFactory.DisplayName,
        Description = "Реляционная база данных: SQL-запросы, схема, вьюхи, правка значений",
        HelpLink = driverFactory.HelpLink,
        DefaultConnectionString = driverFactory.DefaultConnectionString,
        DefaultLanguage = DatasourceLanguage.Sql,
        EditorLanguage = DatasourceEditorLanguage.Sql,
        Hint = "SQL-запрос. Объект из дерева открывается готовым SELECT с лимитом строк; "
               + "значение в гриде правится двойным кликом, изменения применяются через показ SQL.",
        EmptyRequestMessage = "Введите SQL-запрос",
        DefaultGroup = SqlDialectMapping.Dialect(driverFactory.Driver) == SqlDialect.MsSql ? "dbo" : "public",
        Features =
        [
            DatasourceFeature.Query,
            DatasourceFeature.Browse,
            DatasourceFeature.Write,
            DatasourceFeature.CellEdit,
            DatasourceFeature.Views,
            DatasourceFeature.TotalCount,
        ],
    };
}
