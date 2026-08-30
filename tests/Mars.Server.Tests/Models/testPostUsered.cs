using Mars.Data.Entities;

namespace Mars.Server.Tests.Models;

public class testPostUsered : testPost
{
    public Guid UserId { get; set; }
    public UserEntity User { get; set; } = default!;
}
