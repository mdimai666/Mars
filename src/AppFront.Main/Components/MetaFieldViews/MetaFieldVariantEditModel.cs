using System.ComponentModel.DataAnnotations;
using Mars.Shared.Contracts.MetaFields;

namespace AppFront.Shared.Components.MetaFieldViews;

/// <summary>
/// <see cref="MetaFieldVariantResponse"/>
/// </summary>
public class MetaFieldVariantEditModel
{
    public Guid Id { get; set; }
    [Required]
    public string Title { get; set; } = "";
    public string[] Tags { get; set; } = [];
    public float Value { get; set; }
    public bool Disable { get; set; }

    public CreateMetaFieldVariantRequest ToCreateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            Tags = Tags,
            Value = Value,
            Disable = Disable,
        };

    public UpdateMetaFieldVariantRequest ToUpdateRequest()
    => new()
    {
        Id = Id,
        Title = Title,
        Tags = Tags,
        Value = Value,
        Disable = Disable,
    };

    public static MetaFieldVariantEditModel ToModel(MetaFieldVariantResponse response)
    => new()
    {
        Id = response.Id,
        Title = response.Title,
        Disable = response.Disable,
        Value = response.Value,
        Tags = response.Tags.ToArray()
    };
}
