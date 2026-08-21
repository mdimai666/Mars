using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Mars.Host.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

/// <summary>
/// Папка медиа. DB-first: папка существует только если есть запись,
/// физический каталог создаётся вместе с ней. <see cref="Path"/> — путь-префикс
/// от корня upload (задел под S3, где папки = префиксы ключей).
/// </summary>
[DebuggerDisplay("{Name}/{Id}")]
public class MediaFolderEntity : IBasicEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// Имя папки. Совпадает с именем физического каталога.
    /// <example>2026</example>
    /// </summary>
    [Comment("Имя папки")]
    [Required]
    public string Name { get; set; } = default!;

    /// <summary>
    /// Физический путь папки от upload (последний сегмент совпадает с <see cref="Name"/>)
    /// <example>Media/2026</example>
    /// </summary>
    [Comment("Физический путь папки от upload")]
    [Required]
    public string Path { get; set; } = default!;

    [Comment("ИД родительской папки")]
    public Guid? ParentId { get; set; }
    public virtual MediaFolderEntity? Parent { get; set; }
    public virtual ICollection<MediaFolderEntity>? Children { get; set; }

    [Comment("ИД пользователя, создавшего папку")]
    public Guid CreatedBy { get; set; }

    [Comment("Значок папки (зарезервировано)")]
    public string? Icon { get; set; }

    //////////////// Relations

    public virtual ICollection<FileEntity>? Files { get; set; }
}
