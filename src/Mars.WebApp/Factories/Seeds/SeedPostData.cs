using Mars.Core.Extensions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.NavMenus;
using Mars.Host.Data.OwnedTypes.PostTypes;
using Mars.Shared.Contracts.PostTypes;
using Microsoft.EntityFrameworkCore;
using Feature = Mars.Shared.Contracts.PostTypes.PostTypeConstants.Features;

namespace Mars.Factories.Seeds;

public static class SeedPostData
{
    public static async Task SeedFirstData(
        MarsDbContext ef,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        if (ef.PostTypes.Count() > 0) return;

        await SeedPostType(ef);
        await SeedNavMenu(ef);
        await SeedPosts(ef);
    }

    static async Task SeedPostType(MarsDbContext ef)
    {
        int count = ef.PostTypes.Count();
        if (count > 0) return;

        List<PostTypeEntity> list = new();

        var codeSettings = new PostContentSettings
        {
            CodeLang = PostTypeConstants.DefaultPostContentTypes.DefaultCodeTemplate,
            PostContentType = PostTypeConstants.DefaultPostContentTypes.Code,
        };

        list.Add(new PostTypeEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Title = "Записи",
            TypeName = "post",
            PostStatusList = PostStatusEntity.DefaultStatuses(),
            EnabledFeatures = [Feature.Content, Feature.Status, Feature.Tags],
            PostContentType = new PostContentSettings { PostContentType = PostTypeConstants.DefaultPostContentTypes.BlockEditor },
        });

        list.Add(new PostTypeEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Title = "Страница",
            TypeName = "page",
            PostStatusList = PostStatusEntity.DefaultStatuses(),
            EnabledFeatures = [Feature.Content, Feature.Status, Feature.Tags],
            PostContentType = codeSettings.CopyViaJsonConversion<PostContentSettings>(),
        });

        list.Add(new PostTypeEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            Title = "Шаблон",
            TypeName = "template",
            EnabledFeatures = [Feature.Content, Feature.Tags],
            PostContentType = codeSettings.CopyViaJsonConversion<PostContentSettings>(),
        });

        list.Add(new PostTypeEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
            Title = "Blocks",
            TypeName = "block",
            EnabledFeatures = [Feature.Content, Feature.Tags],
            PostContentType = codeSettings.CopyViaJsonConversion<PostContentSettings>(),
        });

        await ef.PostTypes.AddRangeAsync(list);
        await ef.SaveChangesAsync();

    }

    static async Task SeedNavMenu(MarsDbContext ef)
    {
        int count = ef.NavMenus.Count();
        if (count > 0) return;

        List<NavMenuEntity> list = new();

        list.Add(new NavMenuEntity
        {
            Id = Guid.NewGuid(),
            Title = "Главное меню",
            Slug = "top",
            MenuItems = new List<NavMenuItem>
            {
                new NavMenuItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Главная",
                    Url = "/",
                },
                new NavMenuItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Контакты",
                    Url = "/contacts",
                },
            }
        });

        await ef.NavMenus.AddRangeAsync(list);
        await ef.SaveChangesAsync();

    }

    static async Task SeedPosts(MarsDbContext ef)
    {
        int count = ef.Posts.Count();
        if (count > 0) return;

        List<PostEntity> list = new();

        UserEntity user = ef.Users.First();

        var postTypesDict = await ef.PostTypes.ToDictionaryAsync(s => s.TypeName);
        var postTypesDictId = await ef.PostTypes.ToDictionaryAsync(s => s.Id);

        //general pages 
        list.Add(new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = "Index",
            //Type = "page",
            PostTypeId = postTypesDict["page"].Id,
            Slug = "index",
            Content = "<h1>Index page</h1>"
        });
        list.Add(new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = "Admin Index",
            //Type = "page",
            PostTypeId = postTypesDict["page"].Id,
            Slug = "admin",
            Content = "<h1>Admin index page</h1>"
        });

        //required templates
        list.Add(new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = "pageDetail",
            Slug = "pageDetail",
            //Type = "template",
            PostTypeId = postTypesDict["template"].Id,
            Content = "{{{content}}}"
        });
        list.Add(new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = "postDetail",
            Slug = "postDetail",
            //Type = "template",
            PostTypeId = postTypesDict["template"].Id,
            Content = "<ContentWrapper Title=\"{{title}}\">\n\t{{{content}}}\n</ContentWrapper>"
        });

        //hello post
        list.Add(new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = "Hello world!",
            Slug = "helloworld",
            //Type = "post",
            PostTypeId = postTypesDict["post"].Id,
            Content = "<p> hello on Mars!</p>"
        });

        foreach (var post in list)
        {
            post.UserId = user.Id;
            if (postTypesDictId[post.PostTypeId].EnabledFeatures.Contains(Feature.Status))
                post.Status = PostStatusEntity.DefaultStatuses().First().Slug;
            else
                post.Status = "";
        }

        ef.Posts.AddRange(list);

        ef.SaveChanges();
    }
}
