using Mars.Host.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Data;

public class TestMarsDbContext : MarsDbContext
{
    public TestMarsDbContext(DbContextOptions<MarsDbContext> options) : base(options)
    {
    }
}
