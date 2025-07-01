using System.ComponentModel.DataAnnotations;

namespace Mars.Shared.Contracts.MetaFields;

public record MetaFieldResponse
{
    public required Guid Id { get; init; }

    [Display(Name = "Название")]
    public required string Title { get; init; }

    [StringLength(100)]
    [Display(Name = "Тип")]
    public required MetaFieldType Type { get; init; }

    [Display(Name = "Ключ")]
    public required string Key { get; init; }
}
