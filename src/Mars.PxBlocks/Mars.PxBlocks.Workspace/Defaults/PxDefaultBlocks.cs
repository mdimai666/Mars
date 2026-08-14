using Mars.PxBlocks.Shared.Definitions;

namespace Mars.PxBlocks.Workspace.Defaults;

/// <summary>
/// Демо-блоки по умолчанию: пример объявления блоков fluent-API (PxMaster.Define)
/// в классе-наборе PxBlockSet. Имена полей (NUM, TEXT, VAL) сохраняются ради
/// совместимости с уже сохранёнными workspace в localStorage.
/// </summary>
public static class PxDefaultBlocks
{
    public static IReadOnlyList<PxBlockDefinition> Create() =>
        [.. new PxEventBlocks().Definitions, .. new PxDemoBlocks().Definitions];
}

/// <summary>Событийные блоки — аналоги setup()/loop() из Arduino.</summary>
public sealed class PxEventBlocks : PxBlockSet
{
    public PxEventBlocks()
    {
        Add(PxMaster.Define("px_start").Colour("#00838F")
            .Tooltip("Исполняется один раз при запуске — аналог setup() в Arduino")
            .NoPrevious().NoNext().Hat()
            .Message("старт")
            .Message("%1", PxMaster.Do("DO")));

        Add(PxMaster.Define("px_loop").Colour("#00838F")
            .Tooltip("Повторяется после старта, пока не остановят — аналог loop() в Arduino")
            .NoPrevious().NoNext().Hat()
            .Message("цикл")
            .Message("%1", PxMaster.Do("DO")));
    }
}

/// <summary>Демо-набор: значения типов, блоки-приёмники и «создать объект» с мутатором.</summary>
public sealed class PxDemoBlocks : PxBlockSet
{
    public PxDemoBlocks()
    {
        Add(PxMaster.Define("px_demo_number").Output("Number").Colour("#712672")
            .Message("число {NUM}", PxMaster.Number("NUM")));
        Add(PxMaster.Define("px_demo_string").Output("String").Colour("#996600")
            .Message("строка {TEXT}", PxMaster.Text("TEXT", "abc")));
        Add(PxMaster.Define("px_demo_any").Output("Any").Colour("#5C2D91")
            .Message("любое значение"));
        Add(PxMaster.Define("px_demo_object").Output("Object").Colour("#A80000")
            .Message("объект"));

        Add(PxMaster.Define("px_demo_take_number").Colour("#107C10")
            .Message("принять число {VAL}", PxMaster.Value("VAL", "Number")));
        Add(PxMaster.Define("px_demo_take_any").Colour("#107C10")
            .Message("принять любое {VAL}", PxMaster.Value("VAL")));
        Add(PxMaster.Define("px_demo_take_object").Colour("#107C10")
            .Message("принять объект {VAL}", PxMaster.Value("VAL", "Object")));

        Add(PxMaster.Define("px_create_object").Output("Object").Colour("#A80000")
            .Tooltip("Кнопка «+» добавляет пары поле→значение")
            .Message("создать объект")
            .Mutator("px_object_builder"));
    }
}
