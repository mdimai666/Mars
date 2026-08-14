namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>
/// Ядерные событийные блоки — аналоги setup()/loop() из Arduino. Объявления живут
/// в Shared, чтобы их видел и серверный каталог (PxBlockCatalog), и локальный
/// редактор (будущий браузерный рантайм); исполнение — в ядре PxInterpreter.
/// </summary>
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
