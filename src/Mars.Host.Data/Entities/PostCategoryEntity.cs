using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

public class PostCategoryEntity : HierarchyEntity
{
    [ForeignKey(nameof(PostType))]
    public Guid PostTypeId { get; set; }
    public virtual PostTypeEntity? PostType { get; set; }

    [ForeignKey(nameof(Root))]
    public override Guid RootId { get; set; }
    public virtual PostCategoryEntity? Root { get; set; }

    [ForeignKey(nameof(Parent))]
    public override Guid? ParentId { get; set; }
    public virtual PostCategoryEntity? Parent { get; set; }

    [NotMapped]
    public virtual List<PostEntity>? Posts { get; set; }
    public virtual ICollection<PostPostCategoriesEntity>? PostPostCategories { get; set; }

    [ForeignKey(nameof(PostCategoryType))]
    public Guid PostCategoryTypeId { get; set; }
    public virtual PostCategoryTypeEntity? PostCategoryType { get; set; }

    //META
    public virtual ICollection<PostCategoryMetaValueEntity>? PostCategoryMetaValues { get; set; }
    [NotMapped]
    public virtual List<MetaValueEntity>? MetaValues { get; set; }
}
