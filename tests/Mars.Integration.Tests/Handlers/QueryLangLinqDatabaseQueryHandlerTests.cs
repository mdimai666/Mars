using AutoFixture;
using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.WebSite.Models;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.QueryLang.Host.Services;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.Posts;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Options;
using Mars.Test.Common.Constants;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Handlers;

public class QueryLangLinqDatabaseQueryHandlerTests : ApplicationTests
{
    private readonly IQueryLangLinqDatabaseQueryHandler _handler;
    private readonly PageRenderContext _pageContext;

    public QueryLangLinqDatabaseQueryHandlerTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        var sysOptions = new SysOptions() { SiteUrl = "http://localhost" };
        _pageContext = new PageRenderContext()
        {
            Request = new WebClientRequest(new Uri(sysOptions.SiteUrl)),
            SysOptions = sysOptions,
            User = new RenderContextUser(UserConstants.TestUser),
            RenderParam = new RenderParam(),
            IsDevelopment = true,
        };

        _handler = appFixture.ServiceProvider.GetRequiredService<IQueryLangLinqDatabaseQueryHandler>();
    }

    [IntegrationFact]
    public async Task Handle_LinqDatabaseQuery_Success()
    {
        // Arrange
        _ = nameof(QueryLangLinqDatabaseQueryHandler.Handle);

        var expression = "Posts.Where(post.Title==\"111\").ToList()";
        var varibles = new Dictionary<string, object>();

        var createdPosts = _fixture.CreateMany<PostEntity>(3).ToList();
        createdPosts.ForEach(s => s.Title = "111");
        createdPosts[0].Title = "000";
        using var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddRangeAsync(createdPosts);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        // Act
        var result = await _handler.Handle(expression, _pageContext, varibles, default);

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
        var varibles = new Dictionary<string, object>();
        var post = posts.First(post => post.Tags.Contains("v1"));

        // Act
        var metaPostsObject = await _handler.Handle(expression, _pageContext, varibles, default);

        // Assert
        metaPostsObject.Should().NotBeNull();
        var metaPosts = (metaPostsObject as IEnumerable<PostEntity>)!.ToArray();
        metaPosts.Count().Should().Be(1);
        metaPosts[0].Id.Should().Be(post.Id);
        dynamic dMetaPost = metaPosts[0];
        Assert.Equal("v1", dMetaPost.str1);
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
