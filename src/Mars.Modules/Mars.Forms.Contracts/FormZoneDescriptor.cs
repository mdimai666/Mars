namespace Mars.Forms.Contracts;

/// <summary>Зона размещения элементов формы: объявляет провайдер, показывает дизайнер раскладки</summary>
public record FormZoneDescriptor
{
    public required string Key { get; init; }

    public required string Title { get; init; }
}
