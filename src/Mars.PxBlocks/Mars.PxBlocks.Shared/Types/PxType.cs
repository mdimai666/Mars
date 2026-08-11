namespace Mars.PxBlocks.Shared.Types;

/// <summary>
/// Канонический тип стыковки блоков. Источник правды для форм и совместимости;
/// сериализуется в JS как матрица (реестр типов).
/// </summary>
public class PxType
{
    public string Name { get; set; } = "";

    public PxShape Shape { get; set; } = PxShape.Rounded;

    /// <summary>
    /// Типы, с которыми этот тип стыкуется. "*" — с любым.
    /// Точное совпадение имён работает и без регистрации.
    /// </summary>
    public List<string> CompatibleWith { get; set; } = [];
}
