using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace Mars.Factories.Seeds;

public static class SeedUsers
{
    public static void SeedFirstData(UserManager<UserEntity> userManager, MarsDbContext ef)
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

        if (!ef.Users.Any(s => s.Email == "admin@mail.ru"))
        {
            var userTypeId = ef.UserTypes.FirstOrDefault(s => s.TypeName == UserTypeEntity.DefaultTypeName)?.Id
                                ?? ef.UserTypes.First().Id;

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

                UserTypeId = userTypeId,
            };

            IdentityResult result = userManager.CreateAsync(user, "Admin123!").Result;

            if (result.Succeeded)
            {
                userManager.AddToRoleAsync(user, "Admin").Wait();
            }
        }
    }
}
