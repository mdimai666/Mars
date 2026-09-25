using Mars.AiChat.Host.Tools;
using Microsoft.Extensions.AI;

namespace Mars.AiChat.Host.Toolsets;

/// <summary>
/// Доступ к источникам данных: SQL-базы (включая основную БД Mars) и источники из каталога DataSource
/// (file, rest); включён флагом AiChatOption.EnableSqlAccess.
/// Правила работы — в скилле mars-sql.
/// </summary>
public class SqlToolset : IAiToolset
{
    private readonly MarsSqlTools _sqlTools;

    public SqlToolset(MarsSqlTools sqlTools)
    {
        _sqlTools = sqlTools;
    }

    public string Name => "sql";

    public bool IsEnabled(AiToolsetContext ctx) => ctx.Option.EnableSqlAccess;

    public IReadOnlyList<AIFunction> Build(AiToolsetContext ctx) =>
    [
        AIFunctionFactory.Create(_sqlTools.ListDataSources),
        AIFunctionFactory.Create(_sqlTools.GetSourceSchema),
        AIFunctionFactory.Create(_sqlTools.RunQuery),
        AIFunctionFactory.Create(_sqlTools.ExecuteSql),
    ];
}
