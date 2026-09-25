using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Front.Workspaces.Objects;

/// <summary>
/// Привязка блока документа под курсором к операции каталога: форма параметров показывает операцию,
/// которой реально соответствует запрос блока, а значения читает из его текста. У блока, чей запрос
/// не совпал ни с одной известной операцией, формы нет — пользователь вправе написать в документе
/// что угодно.
/// </summary>
public class DocumentBlockBinding
{
    DocumentBlock? _block;

    DatasourceCatalogObject? _operation;

    /// <summary>Блок, к которому привязана форма; null — курсор ещё не попадал в блок.</summary>
    public DocumentBlock? Block => _block;

    /// <summary>Операция каталога, найденная по запросу блока; null — известной операции нет.</summary>
    public DatasourceCatalogObject? Operation => _operation;

    /// <summary>Значения параметров операции, вычитанные из текста блока.</summary>
    public Dictionary<string, string?> Values { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Перепривязаться к блоку, в который попадает строка. Возвращает true, когда привязка
    /// изменилась и форму надо перерисовать. Строки вне блоков (разделитель, шапка документа)
    /// привязку не трогают. Нестрогий режим (пользователь печатает в блоке) не снимает форму
    /// с блока, который перестал совпадать с операцией, — иначе форма мигала бы при дописывании адреса.
    /// </summary>
    public bool Bind(string text, int line, DatasourceCatalog? catalog, bool strict)
    {
        var block = DocumentText.BlockAt(text, line);

        if (block is null) return false;

        var match = catalog is null
            ? null
            : HttpBlockSync.MatchOperation(block.Text, catalog.Groups.SelectMany(group => group.Objects));

        if (match is null && !strict) return false;

        _block = block;

        if (match is null)
        {
            var hadForm = _operation is not null || Values.Count > 0;

            _operation = null;
            Values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            return hadForm;
        }

        var values = HttpBlockSync.ReadValues(block.Text, match);
        var changed = !ReferenceEquals(_operation, match) || !SameValues(Values, values);

        _operation = match;
        Values = values;

        return changed;
    }

    static bool SameValues(Dictionary<string, string?> left, Dictionary<string, string?> right)
        => left.Count == right.Count
            && left.All(pair => right.TryGetValue(pair.Key, out var value) && value == pair.Value);
}
