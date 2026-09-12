using Mars.Forms.Contracts;

namespace Mars.Cms.Contracts.PostTypes;

public class PostTypePresentationEditViewModel
{
    public required PostTypeSummaryResponse PostType { get; init; }
    public required PostTypePresentationResponse Presentation { get; init; }

    /// <summary>
    /// Определение формы редактирования поста (дерево провайдера, нормализованное сохранённой
    /// раскладкой) — исходные данные дизайнера формы. Дескрипторы отдаёт провайдер, поэтому
    /// сохраняются не они, а <see cref="FormLayout"/>.
    /// </summary>
    public FormDefinition? Form { get; init; }

    /// <summary>Сохранённая раскладка формы как она лежит в опциях типа; null — раскладка по умолчанию</summary>
    public FormLayoutSettings? FormLayout { get; init; }
}
