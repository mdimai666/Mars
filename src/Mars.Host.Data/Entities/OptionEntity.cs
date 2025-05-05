using System.ComponentModel.DataAnnotations;
using Mars.Host.Data.Common;
using Mars.Host.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[EntityTypeConfiguration(typeof(OptionEntityConfiguration))]
public class OptionEntity : IBasicEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Required]
    public string Key { get; set; } = default!;

    public string Type { get; set; } = default!;

    [Required]
    public string Value { get; set; } = default!;
}
