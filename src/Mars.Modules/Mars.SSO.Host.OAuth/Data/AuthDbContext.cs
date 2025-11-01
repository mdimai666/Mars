using Mars.SSO.Host.OAuth.Models;
using Microsoft.EntityFrameworkCore;

namespace Mars.SSO.Host.OAuth.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> opts) : base(opts) { }

    //public DbSet<OAuthClient> Clients { get; set; } = default!;
    public DbSet<AuthCode> AuthCodes { get; set; } = default!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;
}
