using System.ComponentModel.DataAnnotations;
using Mars.Contracts.MetaFields;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// <see cref="MetaFieldVariantResponse"/>
/// </summary>
public class MetaFieldVariantEditModel
{
    public Guid Id { get; set; }

    /// <summary>
    /// Стабильный ключ варианта; пустой — сервер сгенерирует из названия
    /// </summary>
    public string Key { get; set; } = "";

    [Required]
    public string Title { get; set; } = "";
    public string[] Tags { get; set; } = [];
    public float Value { get; set; }
    public bool Disable { get; set; }

    public CreateMetaFieldVariantRequest ToCreateRequest()
        => new()
        {
            Id = Id,
            Key = Key,
            Title = Title,
            Tags = Tags,
            Value = Value,
            Disable = Disable,
        };

    public UpdateMetaFieldVariantRequest ToUpdateRequest()
    => new()
    {
        Id = Id,
        Key = Key,
        Title = Title,
        Tags = Tags,
        Value = Value,
        Disable = Disable,
    };

    public static MetaFieldVariantEditModel ToModel(MetaFieldVariantResponse response)
    => new()
    {
        Id = response.Id,
        Key = response.Key,
        Title = response.Title,
        Disable = response.Disable,
        Value = response.Value,
        Tags = response.Tags.ToArray()
    };

    public MetaFieldVariantEditModel Clone()
        => new()
        {
            Id = Guid.NewGuid(),
            Key = Key.Length > 0 ? $"{Key}_copy" : "",
            Title = Title,
            Tags = [.. Tags],
            Value = Value,
            Disable = Disable,
        };
}
