using Mars.Datasource.Abstractions.Models;

namespace Mars.Datasource.Front.Components;

/// <summary>
/// Данные диалога вьюхи: диалект источника, схемы из дерева, тело запроса и предзаполнение имени,
/// когда в редактор загружено определение существующей вьюхи (сценарий «изменить»).
///
/// Тело берётся из редактора рабочей области и в диалоге не правится: предпросмотр DDL должен
/// совпадать с тем, что выполнится, а в живом редакторе внутри диалога это не проверить —
/// у `CodeEditor2` нет события изменения.
/// </summary>
public record CreateViewDialogContent(
    ViewDialect Dialect,
    IReadOnlyList<string> Schemas,
    string Body,
    string? Schema = null,
    string? Name = null,
    bool Replace = false);

/// <summary>
/// Итог диалога: готовый DDL и имя, чтобы после выполнения открыть объект в дереве.
/// </summary>
public record ViewDdlRequest(string Sql, string SchemaName, string ViewName);
