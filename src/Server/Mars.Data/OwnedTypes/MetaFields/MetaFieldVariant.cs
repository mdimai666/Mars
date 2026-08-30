using System.ComponentModel.DataAnnotations;

namespace Mars.Data.OwnedTypes.MetaFields;

/// <summary>
/// OWNED MetaFieldTemplate only
/// </summary>
// [Jsonb]
public class MetaFieldVariant
{
    public Guid Id { get; set; }

    /// <summary>
    /// Стабильный ключ варианта (не переводится); денормализуется в string_short значений
    /// </summary>
    [MaxLength(255)]
    public string Key { get; set; } = "";

    [MaxLength(255)]
    public string Title { get; set; } = default!;

    public List<string> Tags { get; set; } = [];
    public float Value { get; set; }
    public bool Disable { get; set; }

}
