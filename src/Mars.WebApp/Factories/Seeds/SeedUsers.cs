using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mars.Factories.Seeds;

public static class SeedUsers
{
    public static void SeedFirstData(UserManager<UserEntity> userManager, MarsDbContext ef, IConfiguration? configuration = null)
    {
        if (ef.UserTypes.Count() == 0)
        {
            var userType = new UserTypeEntity
            {
                Title = UserTypeEntity.DefaultTypeName,
                TypeName = UserTypeEntity.DefaultTypeName,
                Tags = [],
            };

            ef.UserTypes.Add(userType);
            ef.SaveChanges();
        }

        var adminEmail = configuration?.GetSection("Setup")["AdminEmail"] ?? "admin@mail.ru";
        var adminPassword = configuration?.GetSection("Setup")["AdminPassword"] ?? "Admin123!";
        var adminFirstName = configuration?.GetSection("Setup")["AdminFirstName"] ?? "Admin";

        if (!ef.Users.Any(s => s.Email == adminEmail))
        {
            var userTypeId = ef.UserTypes.AsNoTracking().FirstOrDefault(s => s.TypeName == UserTypeEntity.DefaultTypeName)?.Id
                                ?? ef.UserTypes.AsNoTracking().First().Id;

            var user = new UserEntity
            {
                //Basic
                UserName = adminEmail,
                NormalizedUserName = adminEmail.ToUpper(),
                Email = adminEmail,
                NormalizedEmail = adminEmail.ToUpper(),
                EmailConfirmed = true,
                LockoutEnabled = true,

                //User
                FirstName = adminFirstName,
                LastName = "Adminov",

                UserTypeId = userTypeId,
            };

            IdentityResult result = userManager.CreateAsync(user, adminPassword).Result;

            if (result.Succeeded)
            {
                userManager.AddToRoleAsync(user, "Admin").Wait();
            }
        }
    }
}
