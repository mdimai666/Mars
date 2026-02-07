using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Mars.Host.Data.Common;
using Microsoft.EntityFrameworkCore;
using static Mars.Host.Data.Constants.PostConstants;

namespace Mars.Host.Data.Entities;

[DebuggerDisplay("Post/{Slug}/{Id}/{Title}")]
public class PostEntity : IBasicUserEntity, ISoftDeletable//, IPost, ICommentsSupport, ILikesSupport, IMetaValueSupport
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Comment("Удален в")]
    public DateTimeOffset? DeletedAt { get; set; }

    [Required]
    [Comment("slug")]
    public string Slug { get; set; } = default!;

    [Comment("Теги")]
    public List<string> Tags { get; set; } = [];

    //???
    //public Guid ParentId { get; set; } // TODO: thing About Path [/root/some/path1/path2]
    //public Guid CategoryId { get; set; }

    [Required]
    [Comment("Название")]
    public string Title { get; set; } = default!;

    [Comment("Текст")]
    public string? Content { get; set; }

    [Comment("Отрывок")]
    public string? Excerpt { get; set; } = "";

    //[Comment("Изображение")]
    //[JsonPropertyName("postImage")]
    //public string Image { get; set; } = ""; //TODO: May be relation PostAttachments?

    [Required]
    [Comment("Статус")]
    public string Status { get; set; } = "";

    //[Required]
    //[Comment("Тип")]
    //public string Type { get; set; } = default!;

    /// <summary>
    /// en-us
    /// ru-ru
    /// </summary>
    [Comment("Язык")]
    [MaxLength(LangCodeMaxLength)]
    public string LangCode { get; set; } = "";

    // Relations

    [Comment("ИД пользователя")]
    public Guid UserId { get; set; }
    public virtual UserEntity? User { get; set; }

    //[NotMapped] //вспомогательный, для получения

    [ForeignKey(nameof(PostType))]
    public Guid PostTypeId { get; set; }
    public virtual PostTypeEntity? PostType { get; set; }

    public virtual ICollection<PostFilesEntity>? PostFiles { get; set; }
    [Comment("Файлы")]
    [NotMapped]
    public virtual List<FileEntity>? Files { get; set; }

    public virtual ICollection<PostMetaValueEntity>? PostMetaValues { get; set; }
    [NotMapped]
    public virtual List<MetaValueEntity>? MetaValues { get; set; }

    ////=====================================
    ////Comments
    //public int LikesCount { get; set; } // todo
    //public virtual ICollection<PostLikeEntity>? Likes { get; set; }

    ////=====================================
    ////Likes
    //public int CommentsCount { get; set; }
    //public virtual ICollection<PostCommentEntity>? Comments { get; set; }

    //public PostBasicDto CopyBasic()
    //{
    //    return new PostBasicDto(this);
    //}

    [Timestamp]
    public uint Version { get; init; }

    //=====================================
    //Category
    [NotMapped]
    public virtual List<PostCategoryEntity>? Categories { get; set; }
    public virtual ICollection<PostPostCategoriesEntity>? PostPostCategories { get; set; }
}
