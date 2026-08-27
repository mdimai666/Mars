using Mars.Data.Contexts;
using Mars.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mars.Factories.Seeds;

public static class SeedPostCategories
{
    public static void SeedFirstData(MarsDbContext ef)
    {
        if (ef.PostCategoryTypes.Count() == 0)
        {
            var postCategoryType = new PostCategoryTypeEntity
            {
                Title = "default",
                TypeName = "default",
                Tags = ["default"]
            };

            ef.PostCategoryTypes.Add(postCategoryType);
            ef.SaveChanges();
        }
    }
}
