using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using Mars.Host.Data.Common;

namespace Mars.Host.Data.Entities;
/*
public class PostCommentEntity : IBasicEntity
{
    [Required]
    [Display(Name = "Сообщение")]
    public string MessageHtml { get; set; } = default!;

    //[ForeignKey(nameof(Post))]
    //public Guid? PostId { get; set; }
    //public PostEntity? Post { get; set; }

    [NotMapped]
    public string PlainText => StripHTML(MessageHtml);

    //Comment response

    [ForeignKey(nameof(ParentComment))]
    public Guid? ParentCommentId { get; set; }
    public PostCommentEntity? ParentComment { get; set; }

    public virtual ICollection<PostCommentEntity>? ChildComments { get; set; }

    public static string StripHTML(string input)
    {
        return Regex.Replace(input, "<.*?>", String.Empty);
    }
}

public interface ICommentsSupport : IBasicUserEntity
{
    public int CommentsCount { get; set; }
    public ICollection<PostCommentEntity>? Comments { get; set; }
}
*/
