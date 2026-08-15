using Mars.PxBlocks.Shared.Definitions;
using Mars.PxBlocks.Shared.Toolbox;

namespace Mars.PxBlocks.Host.Shared;

/// <summary>
/// Контекст редактора PxBlocks: зарегистрированная на сервере сущность, которая
/// определяет, какие блоки доступны редактору и как исполняются программы
/// (режим событий, лимиты). Создаётся fluent-ом <c>PxEditorContext.Define("playwright")…</c>,
/// регистрируется в <see cref="Services.IPxEditorContextRegistry"/> при старте.
/// Определения контекста независимы от глобального каталога (IPxBlockCatalog):
/// реализации исполнения (IPxBlockImplement) по-прежнему берутся из каталога
/// (RegisterAssembly доменных сборок). Имя PxContext занято контекстом исполнения
/// интерпретатора (Mars.PxBlocks.Runtime.Execution) — отсюда Editor в названии.
/// </summary>
public sealed class PxEditorContext
{
    private IReadOnlyList<PxBlockDefinition>? _definitionsCache;
    private PxToolbox? _toolboxCache;

    /// <summary>Уникальное имя контекста («playwright», «node-order»…) — ключ во всех API.</summary>
    public required string Name { get; init; }

    /// <summary>Человеческое имя для UI.</summary>
    public string? Title { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// Политика запуска «только события»: фазы в порядке имён (PxEvents.Start /
    /// PxEvents.Loop). null — все верхнеуровневые стеки в порядке workspace.
    /// Пустой список — не исполнять ничего (программа только редактируется).
    /// </summary>
    public IReadOnlyList<string>? EventNames { get; init; }

    /// <summary>Лимит шагов исполнения; 0 — без лимита.</summary>
    public int StepLimit { get; init; }

    /// <summary>Лимит накопленных строк вывода в итоге; 0 — без лимита.</summary>
    public int OutputLimit { get; init; } = 10_000;

    /// <summary>Добавлять ли ядерные событийные блоки Start/Loop в определения контекста.</summary>
    public bool IncludeEventBlocks { get; init; } = true;

    internal IReadOnlyList<PxBlockSet> Sets { get; init; } = [];

    internal IReadOnlyList<PxToolboxCategory> Categories { get; init; } = [];

    internal PxToolbox? CustomToolbox { get; init; }

    /// <summary>Определения блоков контекста: ядерные события (если включены) + наборы.</summary>
    public IReadOnlyList<PxBlockDefinition> EffectiveDefinitions
    {
        get
        {
            if (_definitionsCache == null)
            {
                var definitions = new List<PxBlockDefinition>();
                if (IncludeEventBlocks)
                    definitions.AddRange(new PxEventBlocks().Definitions);
                foreach (var set in Sets)
                    definitions.AddRange(set.Definitions);
                _definitionsCache = definitions;
            }

            return _definitionsCache;
        }
    }

    /// <summary>
    /// Toolbox контекста: свой (если задан) либо дефолт с доменными категориями
    /// перед разделителем и «Переменные»/«Функции» — как в PxBlockCatalog.
    /// </summary>
    public PxToolbox EffectiveToolbox
    {
        get
        {
            if (_toolboxCache != null)
                return _toolboxCache;

            if (CustomToolbox != null)
            {
                _toolboxCache = CustomToolbox;
                return _toolboxCache;
            }

            var toolbox = PxDefaultToolbox.Create();
            if (Categories.Count > 0)
            {
                var index = toolbox.Contents.FindIndex(item => item is PxToolboxSeparator);
                if (index < 0)
                    index = toolbox.Contents.Count;
                toolbox.Contents.InsertRange(index, Categories);
            }

            _toolboxCache = toolbox;
            return _toolboxCache;
        }
    }

    public static PxEditorContextBuilder Define(string name) => new(name);
}

/// <summary>
/// Fluent-построитель <see cref="PxEditorContext"/>: создаётся через
/// <see cref="PxEditorContext.Define"/>, неявно приводится к контексту
/// (конвенция PxMaster/PxBlockBuilder).
/// </summary>
public sealed class PxEditorContextBuilder
{
    private readonly string _name;
    private readonly List<PxBlockSet> _sets = [];
    private readonly List<PxToolboxCategory> _categories = [];
    private string? _title;
    private string? _description;
    private IReadOnlyList<string>? _eventNames;
    private int _stepLimit;
    private int _outputLimit = 10_000;
    private bool _includeEventBlocks = true;
    private PxToolbox? _toolbox;

    internal PxEditorContextBuilder(string name) => _name = name;

    public PxEditorContextBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    public PxEditorContextBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>Режим «только события»: фазы в порядке имён (PxEvents.Start / PxEvents.Loop).</summary>
    public PxEditorContextBuilder Events(params string[] eventNames)
    {
        _eventNames = eventNames;
        return this;
    }

    /// <summary>Лимит шагов исполнения; 0 — без лимита.</summary>
    public PxEditorContextBuilder StepLimit(int limit)
    {
        _stepLimit = limit;
        return this;
    }

    /// <summary>Лимит накопленных строк вывода; 0 — без лимита.</summary>
    public PxEditorContextBuilder OutputLimit(int limit)
    {
        _outputLimit = limit;
        return this;
    }

    /// <summary>Без ядерных событийных блоков Start/Loop (контексты-выражения, фильтры).</summary>
    public PxEditorContextBuilder WithoutEventBlocks()
    {
        _includeEventBlocks = false;
        return this;
    }

    /// <summary>Доменный набор блоков (определения уходят редактору; исполнение — из каталога).</summary>
    public PxEditorContextBuilder Set(PxBlockSet set)
    {
        _sets.Add(set);
        return this;
    }

    public PxEditorContextBuilder Set<TSet>() where TSet : PxBlockSet, new() => Set(new TSet());

    /// <summary>Доменная категория toolbox (встаёт перед разделителем и «Переменные»/«Функции»).</summary>
    public PxEditorContextBuilder Category(PxToolboxCategory category)
    {
        _categories.Add(category);
        return this;
    }

    /// <summary>Полностью свой toolbox вместо дефолта (контексты-выражения, фильтры).</summary>
    public PxEditorContextBuilder Toolbox(PxToolbox toolbox)
    {
        _toolbox = toolbox;
        return this;
    }

    public PxEditorContext Build() => new()
    {
        Name = _name,
        Title = _title,
        Description = _description,
        EventNames = _eventNames,
        StepLimit = _stepLimit,
        OutputLimit = _outputLimit,
        IncludeEventBlocks = _includeEventBlocks,
        Sets = _sets.ToArray(),
        Categories = _categories.ToArray(),
        CustomToolbox = _toolbox
    };

    public static implicit operator PxEditorContext(PxEditorContextBuilder builder) => builder.Build();
}
