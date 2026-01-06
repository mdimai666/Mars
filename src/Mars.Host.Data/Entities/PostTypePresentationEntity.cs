using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

public class PostTypePresentationEntity
{
    [Key]
    [Comment("ИД")]
    [ForeignKey(nameof(PostType))]
    public Guid PostTypeId { get; set; }
    public virtual PostTypeEntity? PostType { get; set; }

    public string? ListViewTemplateSourceUri { get; set; }
}
