namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>
/// Ядерные событийные блоки — аналоги setup()/loop() из Arduino. Объявления живут
/// в Shared, чтобы их видел и серверный каталог (PxBlockCatalog), и локальный
/// редактор (будущий браузерный рантайм); исполнение — в ядре PxInterpreter.
/// Отдельные определения доступны фабриками <see cref="CreateStart"/>/<see cref="CreateLoop"/>
/// (контексты с неполным набором событий, например браузерные сценарии — только Start).
/// </summary>
public sealed class PxEventBlocks : PxBlockSet
{
    public PxEventBlocks()
    {
        Add(CreateStart());
        Add(CreateLoop());
    }

    /// <summary>Определение блока «старт» (исполняется один раз при запуске).</summary>
    public static PxBlockDefinition CreateStart() => PxMaster.Define("core.events.start").Colour("#00838F")
        .Tooltip("Исполняется один раз при запуске — аналог setup() в Arduino")
        .NoPrevious().NoNext().Hat()
        .Message("старт")
        .Message("%1", PxMaster.Do("DO"));

    /// <summary>Определение блока «цикл» (повторяется, пока не остановят).</summary>
    public static PxBlockDefinition CreateLoop() => PxMaster.Define("core.events.loop").Colour("#00838F")
        .Tooltip("Повторяется после старта, пока не остановят — аналог loop() в Arduino")
        .NoPrevious().NoNext().Hat()
        .Message("цикл")
        .Message("%1", PxMaster.Do("DO"));
}
