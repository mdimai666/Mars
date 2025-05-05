using System.ComponentModel.DataAnnotations;

namespace Mars.Host.Data.OwnedTypes.MetaFields;

/// <summary>
/// OWNED MetaFieldTemplate only
/// </summary>
// [Jsonb]
public class MetaFieldVariant
{
    public Guid Id { get; set; }

    [MaxLength(255)]
    public string Title { get; set; } = default!;

    public List<string> Tags { get; set; } = [];
    public float Value { get; set; }
    public bool Disable { get; set; }

}
