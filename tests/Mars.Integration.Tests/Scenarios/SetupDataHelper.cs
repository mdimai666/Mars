using AutoFixture;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Repositories;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Scenarios;

internal class SetupDataHelper
{
    public IFixture _fixture = new Fixture();
    ApplicationFixture _appFixture;

    public SetupDataHelper(ApplicationFixture appFixture)
    {
        _appFixture = appFixture;
        _fixture.Customize(new FixtureCustomize());
    }

    internal async Task<PostTypeEntity> SetupPostType(string postTypeName = "myType", List<MetaFieldEntity>? metaFields = null)
    {
        var postType = _fixture.Create<PostTypeEntity>();
        postType.TypeName = postTypeName;
        postType.MetaFields = metaFields ?? [];

        var ef = _appFixture.DbFixture.DbContext;
        ef.PostTypes.Add(postType);
        await ef.SaveChangesAsync();

        return postType;
    }

    internal async Task<PostDetail[]> SetupPosts(string postTypeName = "myType",
                                                    int postCount = 3,
                                                    Action<PostEntity, int>? postModifyFunc = null,
                                                    Func<PostEntity, int, List<MetaValueEntity>>? metaValuesCreateFunc = null)
    {
        var ef = _appFixture.DbFixture.DbContext;
        var postType = await ef.PostTypes.Include(s => s.MetaFields).FirstAsync();

        return await SetupPosts(postType, postCount, postModifyFunc, metaValuesCreateFunc);
    }

    internal async Task<PostDetail[]> SetupPosts(PostTypeEntity postType,
                                                int postCount = 3,
                                                Action<PostEntity, int>? postModifyFunc = null,
                                                Func<PostEntity, int, List<MetaValueEntity>>? metaValuesCreateFunc = null)
    {
        var ef = _appFixture.DbFixture.DbContext;

        var mfdict = postType.MetaFields!.ToDictionary(s => s.Key);

        var posts = _fixture.CreateMany<PostEntity>(postCount);
        var i = 0;
        foreach (var post in posts)
        {
            post.Title = $"Post {i}";
            post.PostTypeId = postType.Id;
            postModifyFunc?.Invoke(post, i);
            post.PostType = postType;
            post.MetaValues = metaValuesCreateFunc(post, i) ?? [];

            if (post.MetaValues.Count != postType.MetaFields.Count)
                throw new Exception("post.MetaValues.Count and postType.MetaFields.Count must same count");
            for (int mi = 0; mi < postType.MetaFields.Count; mi++)
            {
                post.MetaValues.ElementAt(mi).MetaFieldId = postType.MetaFields.ElementAt(mi).Id;
            }
            post.PostType = null;
            i++;
        }

        ef.Posts.AddRange(posts);
        await ef.SaveChangesAsync();

        var ps = _appFixture.ServiceProvider.GetRequiredService<IPostRepository>();
        var postList = await ps.ListAllDetail(new() { Type = postType.TypeName }, default);

        return postList.ToArray();
    }

    internal async Task<(PostTypeEntity postType, PostDetail[] posts)>
            SetupPostTypeAndPosts(string postTypeName = "myType",
                                    List<MetaFieldEntity>? metaFields = null,
                                    int postCount = 3,
                                    Action<PostEntity, int>? postModifyFunc = null,
                                    Func<PostEntity, int, List<MetaValueEntity>>? metaValuesCreateFunc = null)
    {
        var postType = await SetupPostType(postTypeName, metaFields);
        var posts = await SetupPosts(postType, postCount, postModifyFunc, metaValuesCreateFunc);

        return (postType, posts);
    }
}
