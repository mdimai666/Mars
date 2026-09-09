using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions;

/// <summary>
/// Слияние сохранённой раскладки формы с раскладкой по умолчанию от провайдера.
/// Тот же алгоритм, что уже применяется к колонкам грида постов, только один на всех:
/// сохранённый порядок и настройки выигрывают, дескрипторы всегда свежие из defaults,
/// неизвестные ключи отбрасываются, недостающие дописываются в конец своей зоны,
/// каждый ключ встречается ровно один раз.
/// </summary>
public interface IFormDefinitionNormalizer
{
    IReadOnlyCollection<FormItem> Normalize(IReadOnlyCollection<FormItem>? saved,
                                            IReadOnlyCollection<FormItem> defaults,
                                            FormNormalizeOptions? options = null);
}

public record FormNormalizeOptions
{
    /// <summary>Предельная вложенность секций; дети глубже поднимаются на уровень предела</summary>
    public int MaxSectionDepth { get; init; } = 1;
}
