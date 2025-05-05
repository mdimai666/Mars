using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PluginExample.Data.Entities;

namespace PluginExample.Data.Seeds;

public class PluginNewsSeed
{
    public const string InitialPostTitle = "Hello, World - from plugin!";

    public static async Task SeedFirstData(
        MyPluginDbContext ef,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        string contentRootPath)
    {
        int count = await ef.News.CountAsync();
        if (count > 0) return;

        var firstUser = ef.Users.First();

        var post = new PluginNewsEntity()
        {
            Title = InitialPostTitle,
            Content = "content",
            UserId = firstUser.Id,
        };

        ef.News.Add(post);
        await ef.SaveChangesAsync();
    }
}
