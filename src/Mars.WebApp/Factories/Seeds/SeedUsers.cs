using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace Mars.Factories.Seeds;

public static class SeedUsers
{
    public static void SeedFirstData(UserManager<UserEntity> userManager, MarsDbContext ef)
    {
        if (!ef.Users.Any(s => s.Email == "admin@mail.ru"))
        {
            var user = new UserEntity
            {
                //Basic
                UserName = "admin",
                NormalizedUserName = "ADMIN@MAIL.RU",
                Email = "admin@mail.ru",
                NormalizedEmail = "ADMIN@MAIL.RU",
                EmailConfirmed = true,
                LockoutEnabled = true,

                //User
                FirstName = "Admin",
                LastName = "Adminov",
                MiddleName = "A",
                BirthDate = new DateTime(1989, 1, 1),
            };

            IdentityResult result = userManager.CreateAsync(user, "Admin123!").Result;

            if (result.Succeeded)
            {
                userManager.AddToRoleAsync(user, "Admin").Wait();
            }
        }
    }
}
