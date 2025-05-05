using Mars.Host.Data.Entities;

namespace Test.Mars.Host.Models;

public class testPostUsered : testPost
{
    public Guid UserId { get; set; }
    public UserEntity User { get; set; } = default!;
}
