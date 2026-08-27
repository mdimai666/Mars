using System.Text.Json.Nodes;
using Mars.Core.Extensions;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Data.OwnedTypes.NavMenus;
using Mars.Contracts.MetaFields;
using Mars.Contracts.PostTypes;
using Microsoft.EntityFrameworkCore;
using Feature = Mars.Contracts.PostTypes.PostTypeConstants.Features;

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

        list.Add(new PostTypeEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Title = "Записи",
            TypeName = "post",
            Statuses = PostStatusEntity.DefaultStatuses(),
            EnabledFeatures = [Feature.Content, Feature.Status, Feature.Tags],
            MetaFields =
            [
                new MetaFieldEntity
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.Now,
                    Title = FeatureFieldsCatalog.ContentFieldTitle,
                    Key = FeatureFieldsCatalog.ContentFieldKey,
                    Type = EMetaFieldType.Text,
                    IsNullable = true,
                    Options = new JsonObject
                    {
                        [FeatureFieldsCatalog.FeatureKeyOption()] = FeatureFieldsCatalog.Content,
                        [MetaFieldEditorCatalog.EditorOption()] = MetaFieldEditorCatalog.BlockEditor,
                    },
                    Order = 0,
                    Tags = [],
                    Variants = [],
                },
            ],
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

        var statuses = await ef.PostStatuses.ToListAsync();
        var firstStatusByType = statuses.GroupBy(s => s.PostTypeId)
                                        .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Order).First().Id);

        foreach (var post in list)
        {
            post.UserId = user.Id;
            if (postTypesDictId[post.PostTypeId].EnabledFeatures.Contains(Feature.Status)
                && firstStatusByType.TryGetValue(post.PostTypeId, out var statusId))
                post.StatusId = statusId;
        }

        ef.Posts.AddRange(list);

        ef.SaveChanges();
    }
}
