using AutoFixture;
using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Host.Shared.Repositories;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Scenarios;
using Mars.QueryLang.Host.Services;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.Posts;
using Mars.Shared.Contracts.PostTypes;
using Mars.Test.Common.Constants;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Handlers;

public class QueryLangLinqDatabaseQueryHandlerTests : ApplicationTests
{
    private readonly SetupDataHelper _setupDataHelper;
    private readonly IQueryLangLinqDatabaseQueryHandler _handler;

    public QueryLangLinqDatabaseQueryHandlerTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _setupDataHelper = new SetupDataHelper(appFixture);
        _handler = appFixture.ServiceProvider.GetRequiredService<IQueryLangLinqDatabaseQueryHandler>();
    }

    [IntegrationFact]
    public async Task Handle_LinqDatabaseQuery_Success()
    {
        // Arrange
        _ = nameof(QueryLangLinqDatabaseQueryHandler.Handle);

        var expression = "Posts.Where(post.Title==\"111\").ToList()";

        var createdPosts = _fixture.CreateMany<PostEntity>(3).ToList();
        createdPosts.ForEach(s => s.Title = "111");
        createdPosts[0].Title = "000";
        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddRangeAsync(createdPosts);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        // Act
        var result = await _handler.Handle(expression, new(), default);

        // Assert
        result.Should().NotBeNull();
        var pgResult = (result as IEnumerable<PostEntity>)!;
        pgResult.Count().Should().Be(2);
    }

    [IntegrationFact]
    public async Task Handle_LinqForMetaField_ShouldWork()
    {
        // Arrange
        _ = nameof(QueryLangLinqDatabaseQueryHandler.Handle);

        var (postTypeDetail, posts) = await SetupPostType();
        var mf = postTypeDetail.MetaFields.First();

        var expression = $"{postTypeDetail.TypeName}.Where(post.str1==\"v1\").ToList()";
        var post = posts.First(post => post.Tags.Contains("v1"));

        // Act
        var metaPostsObject = await _handler.Handle(expression, new(), default);

        // Assert
        metaPostsObject.Should().NotBeNull();
        var metaPosts = (metaPostsObject as IEnumerable<PostEntity>)!.ToArray();
        metaPosts.Count().Should().Be(1);
        metaPosts[0].Id.Should().Be(post.Id);
        dynamic dMetaPost = metaPosts[0];
        Assert.Equal("v1", dMetaPost.str1);
    }

    [IntegrationFact]
    public async Task Handle_LinqUnionOnDirectEntities_ShouldWork()
    {
        // Arrange
        var postTypeName = "myType";
        var (postType, posts) = await _setupDataHelper.SetupPostTypeAndPosts(
            postTypeName,
            [new() { Id = Guid.NewGuid(), Type = EMetaFieldType.Bool, Key = "pinned", Title = "Pinned" }],
            4,
            (post, i) => post.Slug = $"post-{i}",
            (post, i) => [new() { Type = EMetaFieldType.Bool, Bool = i == 1 || i == 2 }]);

        string[] expectOrder = ["post-1", "post-0", "post-2", "post-3"]; // leading one pinned post

        var ef = AppFixture.DbFixture.DbContext;

        var query = ef.Posts.Where(s => s.PostType.TypeName == postTypeName);

        var unionPosts = await query.OrderByDescending(s => s.Slug)
                                        .Union(query.Where(s => s.Slug == "post-1").Take(1))
                                        .ToListAsync();

        var expression = $$"""
                            Post.Where(post.PostType.TypeName == "{{postTypeName}}")
                                .OrderByDescending(Slug)
                                .Union(
                                    Post.Where(post.PostType.TypeName == "{{postTypeName}}")
                                        .Where(post.Slug=="post-1")
                                        .Take(1))
                                .ToList()
                            """;

        // Act
        var result = await _handler.Handle(expression, new(), default);
        var expPosts = (result as IEnumerable<PostEntity>)!;

        // Act
        unionPosts.Select(s => s.Slug).Should().BeEquivalentTo(expectOrder);
        expPosts.Select(s => s.Slug).Should().BeEquivalentTo(expectOrder);

    }

    async Task<(PostTypeDetail postTypeDetail, PostDetail[] posts)> SetupPostType(string typeName = "mytype", int createPostCount = 3)
    {
        var postType = _fixture.Create<CreatePostTypeRequest>().ToQuery() with { TypeName = typeName };
        var metaField = _fixture.Create<MetaFieldDto>() with { Key = "str1", ParentId = Guid.Empty, Type = MetaFieldType.String };
        postType = postType with { MetaFields = [metaField] };
        var pts = AppFixture.ServiceProvider.GetRequiredService<IPostTypeRepository>();
        var ps = AppFixture.ServiceProvider.GetRequiredService<IPostRepository>();
        var postTypeId = await pts.Create(postType, default);

        var posts = _fixture.CreateMany<CreatePostRequest>(createPostCount).Select((post, i) => post.ToQuery(UserConstants.TestUserId, postType.MetaFields.ToDictionary(s => s.Id)) with
        {
            Title = $"title - {i}",
            Type = postType.TypeName,
            Tags = [$"v{i}"],
            MetaValues = [_fixture.Create<ModifyMetaValueDetailQuery>() with {
                MetaFieldId = metaField.Id,
                MetaField = metaField,
                StringShort = $"v{i}",
            }]
        }).ToArray();

        var postTypeDetail = (await pts.GetDetailByName(postType.TypeName, default))!;

        foreach (var post in posts) await ps.Create(post, default);

        var postList = await ps.ListAllDetail(new() { Type = postType.TypeName }, default);

        return (postTypeDetail, postList.ToArray());
    }
}
