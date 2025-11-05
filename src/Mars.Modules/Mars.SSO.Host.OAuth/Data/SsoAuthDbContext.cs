using Mars.SSO.Host.OAuth.Models;
using Microsoft.EntityFrameworkCore;

namespace Mars.SSO.Host.OAuth.Data;

public class SsoAuthDbContext : DbContext
{
    public SsoAuthDbContext(DbContextOptions<SsoAuthDbContext> opts) : base(opts) { }

    //public DbSet<OAuthClient> Clients { get; set; } = default!;
    public DbSet<AuthCode> AuthCodes { get; set; } = default!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;
}
