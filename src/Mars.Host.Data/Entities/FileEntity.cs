using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Mars.Host.Data.Common;
using Mars.Host.Data.Configurations;
using Mars.Host.Data.OwnedTypes.Files;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[DebuggerDisplay("{FileName}/{Id}")]
[EntityTypeConfiguration(typeof(FileEntityConfiguration))]
public class FileEntity : IBasicUserEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// Имя файла
    /// <example>example_file.jpg</example>
    /// </summary>
    [Comment("Имя файла")]
    [Required]
    public string FileName { get; set; } = default!;

    /// <summary>
    /// path to file without /Upload and 
    /// </summary>
    [Comment("Физический путь файла от upload")]
    [Required]
    public string FilePhysicalPath { get; set; } = default!;

    /// <summary>
    /// File virtual path /Upload 
    /// </summary>
    [Comment("Виртуальный путь файла")]
    [Required]
    public string FileVirtualPath { get; set; } = default!;

    [Comment("Размер файла в байтах")]
    public ulong FileSize { get; set; }

    /// <summary>
    /// Расширение файла. Без ведущей точки.
    /// </summary>
    [Comment("Расширение файла. Без ведущей точки.")]
    [RegularExpression(@"^[^.].*")]
    [Required(AllowEmptyStrings = true)]
    public string FileExt { get; set; } = default!;
    //public EFileType FileType { get; set; }


    /// <summary>
    /// ????????? что бы придумать? -> Virtual path
    /// </summary>
    //public string FileGroup { get; set; } = default!;

    [Comment("Мета поля")]
    [Column(TypeName = "jsonb")]
    public virtual FileEntityMeta Meta { get; set; } = new();


    //////////////// Relations

    public virtual ICollection<PostFilesEntity>? PostFiles { get; set; }
    [NotMapped]
    public virtual List<PostEntity>? Posts { get; set; }

    [Comment("ИД пользователя")]
    public Guid UserId { get; set; }
    public virtual UserEntity? User { get; set; }

}

//public enum EFileType
//{
//    None,
//    UserAvatar,
//    ApplicationAttachment,
//    Document,
//    PostAttachment,
//    ZayavkaReport,
//    PacientAttachment,
//    Media,
//    Meeting

//}
