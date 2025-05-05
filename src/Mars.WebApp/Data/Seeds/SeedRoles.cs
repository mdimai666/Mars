using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;

namespace Mars.Data.Seeds;

public static class SeedRoles
{
    public static void SeedFirstData(MarsDbContext ef)
    {
        if (ef.Roles.Count() > 0) return;

        string[] defaultRoles = ["Viewer", "Admin", "Manager", "Developer"];

        var entities = defaultRoles.Select(name => new RoleEntity
        {
            Name = name,
            NormalizedName = name.ToUpper(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsActive = true,
        });

        ef.Roles.AddRange(entities);
        ef.SaveChanges();
    }
}
