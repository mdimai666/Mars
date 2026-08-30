using Mars.Contracts.Models.Interfaces;

namespace Mars.Admin.Framework.Services;

// for Develop
public class TitleEntity : IBasicEntity
{
    public string Title { get; set; } = default!;
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}
