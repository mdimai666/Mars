using Mars.Data.Entities;

namespace Test.Mars.Server.Models;

public class testPostUsered : testPost
{
    public Guid UserId { get; set; }
    public UserEntity User { get; set; } = default!;
}
