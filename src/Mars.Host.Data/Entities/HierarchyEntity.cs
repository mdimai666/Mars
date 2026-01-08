using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mars.Host.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

/// <summary>
/// Не используется.
/// Еще на этапе задумки.
///
/// - возожно стоит держать кеш
/// </summary>
public abstract class HierarchyEntity : IBasicEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Required]
    [Comment("slug")]
    public virtual string Slug { get; set; }

    //[Required]
    //public string PathName { get; set; } = default!;

    //[Required]
    //public string Paths { get; set; } = default!;

    /// <summary>
    /// /{rootId}/{parentId}/{id}/
    /// </summary>
    [Required]
    public string Path { get; set; } = default!;

    /// <summary>
    /// /news/tech/ai
    /// </summary>
    [Required]
    public string SlugPath { get; set; } = default!;

    public Guid RootHierarchyId { get; set; }
    public Guid[] HierarchyPathIds { get; set; } = [];

    public virtual HierarchyEntity? RootHierarchy { get; set; }
    public virtual ICollection<HierarchyEntity>? HierarchyEntities { get; set; }

    public int LevelsCount { get; set; }

    //public Guid RootElementId { get; set; }
    //public Guid[] ElementsPathIds { get; set; } = [];

    [Comment("Отключен")]
    public bool Disabled { get; set; }

    //[Comment("Теги")]
    //public List<string> Tags { get; set; } = [];
}

public class PostCategoryEntity : HierarchyEntity
{
    [ForeignKey(nameof(Post))]
    public Guid PostId { get; set; }
    public virtual PostEntity? Post { get; set; }

    [ForeignKey(nameof(Hierarchy))]
    public Guid HierarchyEntityId { get; set; }
    public virtual HierarchyEntity? Hierarchy { get; set; }

    //------------------

    ////////////////////
    [ForeignKey(nameof(PostType))]
    public Guid PostTypeId { get; set; }
    public virtual PostTypeEntity? PostType { get; set; }
    ////////////////////
    [ForeignKey(nameof(Parent))]
    public Guid? ParentId { get; set; }
    public PostHierarchyEntity? Parent { get; set; }
    ////////////////////

    [ForeignKey(nameof(RootPost))]
    public Guid RootPostId { get; set; }
    public virtual PostEntity? RootPost { get; set; }

    public virtual ICollection<PostEntity>? PostEntities { get; set; }
    public virtual ICollection<PostPostCategoryEntity>? PostEntities2 { get; set; }
}

public class PostPostCategoryEntity
{
    [ForeignKey(nameof(Post))]
    public Guid PostId { get; set; }
    public virtual PostEntity? Post { get; set; }

    [ForeignKey(nameof(PostCategory))]
    public Guid PostCategoryId { get; set; }
    public virtual PostCategoryEntity? PostCategory { get; set; }
}

public class AAA_ExampleUsage
{
    public void X()
    {
        PostEntity post = default!;
        PostEntity subPost = default!;

        Guid hid = default!;

        HierarchyEntity x = new()
        {
            Id = hid,
            RootHierarchyId = hid,
            HierarchyPathIds = [hid, post.Id, subPost.Id],
        };

    }
}
