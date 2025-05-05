using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Host.Data.Entities;

public class PostFilesEntity
{
    [ForeignKey(nameof(Post))]
    public Guid PostId { get; set; }
    public virtual PostEntity? Post { get; set; }

    [ForeignKey(nameof(FileEntity))]
    public Guid FileEntityId { get; set; }
    public virtual FileEntity? FileEntity { get; set; }
}
