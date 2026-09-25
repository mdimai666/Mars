namespace Mars.Datasource.Contracts.Config;

/// <summary>
/// Профиль типа источника: всё, что нужно форме настроек и рабочей области, кроме самих данных.
/// Провайдер описывает себя сам, поэтому новый тип источника не требует правок ядра, контроллера
/// и фронта — они читают профиль, а не сравнивают <see cref="DatasourceConfig.Kind"/>.
/// </summary>
public class DatasourceKindProfile
{
    public string Kind { get; set; } = DatasourceKind.Sql;

    /// <summary>Вариант провайдера внутри типа источника; пустой, если вариант один (file, rest).</summary>
    public string Driver { get; set; } = "";

    /// <summary>Подпись типа источника в форме настроек («SQL — реляционная база»).</summary>
    public string KindLabel { get; set; } = "";

    /// <summary>Подпись варианта провайдера внутри типа («PostgreSQL»); у типа без вариантов — своя.</summary>
    public string Label { get; set; } = "";

    /// <summary>Пояснение к подписи: что это за источник.</summary>
    public string Description { get; set; } = "";

    public string HelpLink { get; set; } = "";

    /// <summary>Заготовка строки подключения; пустая — строка подключения источнику не нужна.</summary>
    public string DefaultConnectionString { get; set; } = "";

    /// <summary>Язык запроса по умолчанию: значение <see cref="DatasourceLanguage"/>.</summary>
    public string DefaultLanguage { get; set; } = DatasourceLanguage.Sql;

    /// <summary>Подсветка редактора: значение <see cref="DatasourceEditorLanguage"/>.</summary>
    public string EditorLanguage { get; set; } = DatasourceEditorLanguage.Sql;

    /// <summary>Подсказка под редактором: синтаксис запроса и примеры. Многострочный текст.</summary>
    public string Hint { get; set; } = "";

    /// <summary>Сообщение, когда текст запроса пустой.</summary>
    public string EmptyRequestMessage { get; set; } = "Введите запрос";

    /// <summary>Клик по объекту дерева переходит к его тексту в документе, а не выполняет запрос.</summary>
    public bool OpensAsDocument { get; set; }

    /// <summary>Имя документа запросов пользователя в data-корне; null — документа нет.</summary>
    public string? DocumentName { get; set; }

    /// <summary>Группа дерева, раскрытая по умолчанию; пустая — раскрыты все.</summary>
    public string DefaultGroup { get; set; } = "";

    /// <summary>
    /// Сворачивать группы по умолчанию (оставляя <see cref="DefaultGroup"/>) только когда объектов
    /// в каталоге больше этого числа; 0 — сворачивать всегда. Маленький каталог виден целиком.
    /// </summary>
    public int CollapseGroupsAbove { get; set; }

    /// <summary>Возможности источника: значения <see cref="DatasourceFeature"/>.</summary>
    public List<string> Features { get; set; } = [];

    /// <summary>Поля формы настроек кроме строки подключения.</summary>
    public List<DatasourceSettingField> Settings { get; set; } = [];

    public bool Has(string feature) => Features.Contains(feature, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Возможности источника. Набор строк, а не список bool: новый вид источника добавляет свою
/// возможность, не меняя общий контракт и не заводя флаг, который ничего не значит остальным.
/// </summary>
public static class DatasourceFeature
{
    /// <summary>Источник выполняет запрос текстом.</summary>
    public const string Query = "query";

    /// <summary>Открытие объекта дерева показывает его данные.</summary>
    public const string Browse = "browse";

    /// <summary>Источник можно писать (запрос на изменение требует подтверждения).</summary>
    public const string Write = "write";

    /// <summary>Правка значения в гриде: источник умеет точечное изменение строки.</summary>
    public const string CellEdit = "cellEdit";

    /// <summary>Управление вьюхами: создать, заменить, удалить, показать определение.</summary>
    public const string Views = "views";

    /// <summary>Документ запросов пользователя (вкладка-документ и группы из него).</summary>
    public const string Document = "document";

    /// <summary>Каталог операций собирается из внешнего описания источника.</summary>
    public const string Discover = "discover";

    /// <summary>Общее число записей объекта отдельным запросом («строк: N из M»).</summary>
    public const string TotalCount = "count";
}

/// <summary>
/// Языки подсветки редактора. Значения совпадают с идентификаторами языков Monaco из белого
/// списка <c>MarsCodeEditor2.CodeEditor2.Language</c>.
/// </summary>
public static class DatasourceEditorLanguage
{
    public const string Sql = "sql";
    public const string CSharp = "csharp";
    public const string PlainText = "plaintext";
    public const string Json = "json";
    public const string GraphQL = "graphql";

    /// <summary>Документ запросов .http: язык регистрирует MarsCodeEditor2 (свой монарх и CodeLens).</summary>
    public const string Http = "http";
}

/// <summary>Действие источника: утилита, которую провайдер умеет выполнять по идентификатору.</summary>
public class DatasourceActionDescriptor
{
    public string Id { get; set; } = "";

    /// <summary>Подпись кнопки.</summary>
    public string Label { get; set; } = "";

    /// <summary>Пояснение во всплывающей подсказке.</summary>
    public string Description { get; set; } = "";
}
