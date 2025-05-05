using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

/// <summary>
/// Не используется.
/// Еще на этапе задумки.
/// </summary>
public class HierarchyEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public string PathName { get; set; } = default!;

    [Required]
    public string Paths { get; set; } = default!;

    public Guid RootHierarchyId { get; set; }
    public Guid[] HierarchyPathIds { get; set; } = [];

    public virtual HierarchyEntity? RootHierarchy { get; set; }
    public virtual ICollection<HierarchyEntity>? HierarchyEntities { get; set; }

    public int LevelsCount { get; set; }

    //public Guid RootElementId { get; set; }
    //public Guid[] ElementsPathIds { get; set; } = [];
}

public class PostHierarchyEntity
{
    [ForeignKey(nameof(Post))]
    public Guid PostId { get; set; }
    public virtual PostEntity? Post { get; set; }

    [ForeignKey(nameof(Hierarchy))]
    public Guid HierarchyEntityId { get; set; }
    public virtual HierarchyEntity? Hierarchy { get; set; }

    //--

    [ForeignKey(nameof(RootPost))]
    public Guid RootPostId { get; set; }
    public virtual PostEntity? RootPost { get; set; }



    public virtual ICollection<PostEntity>? PostEntities { get; set; }
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
