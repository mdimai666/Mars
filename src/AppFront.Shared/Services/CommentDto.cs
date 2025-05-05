using Mars.Core.Extensions;
using Mars.Shared.Contracts.Users;
using Mars.Shared.Models.Interfaces;

namespace AppFront.Shared.Services;

//Temp: after rework - delete
public class CommentDto : IBasicEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }

    public string MessageHtml { get; set; } = default!;

    public Guid? ParentCommentId { get; set; }

    public List<CommentDto>? ChildComments { get; set; }

    public UserSummaryResponse User { get; set; } = default!;
    public Guid? PostId { get; set; }

    public string PlainText => MessageHtml.StripHTML();

}
