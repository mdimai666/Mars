using AutoFixture;
using FluentAssertions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.Posts;
using Mars.Cms.Contracts.PostTypes;
using Mars.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Scenarios;
using Mars.QueryLang.Host.Services;
using Mars.QueryLang.Services;
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
    public async Task Handle_SelectChain_MaterializesProjection()
    {
        // Arrange
        // "222" — уникальный маркер: БД общая на класс-фикстуру, сид других тестов использует "111"
        var expression = "Posts.Where(post.Title==\"222\").Select(Title).ToList()";

        var createdPosts = _fixture.CreateMany<PostEntity>(3).ToList();
        createdPosts.ForEach(s => s.Title = "222");
        createdPosts[0].Title = "000";
        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddRangeAsync(createdPosts);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        // Act
        var result = await _handler.Handle(expression, new(), default);

        // Assert
        var titles = (result as IEnumerable<string>)!.ToList();
        titles.Should().HaveCount(2);
        titles.Should().OnlyContain(t => t == "222");
    }

    [IntegrationFact]
    public async Task Handle_Predicates_AnyAll_TranslateOnPostgres()
    {
        // Arrange — "agg-333" уникальный маркер (БД общая на класс-фикстуру)
        var createdPosts = _fixture.CreateMany<PostEntity>(3).ToList();
        createdPosts.ForEach(s => s.Title = "agg-333");
        createdPosts[0].Title = "agg-000";
        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddRangeAsync(createdPosts);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        var filter = "Posts.Where(post.Title.StartsWith(\"agg-\"))";

        // Act
        var any = await _handler.Handle($"{filter}.Any(post.Title==\"agg-333\")", new(), default);
        var all = await _handler.Handle($"{filter}.All(post.Title.Length==7)", new(), default);
        var anyEmpty = await _handler.Handle($"{filter}.Any()", new(), default);

        // Assert
        any.Should().Be(true);
        all.Should().Be(true);
        anyEmpty.Should().Be(true);
    }

    [IntegrationFact]
    public async Task Handle_MaxMinDistinct_TranslateOnPostgres()
    {
        // Arrange — "minmax-444" уникальный маркер (БД общая на класс-фикстуру)
        var createdPosts = _fixture.CreateMany<PostEntity>(3).ToList();
        createdPosts.ForEach(s => s.Title = "minmax-444");
        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddRangeAsync(createdPosts);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        var filter = "Posts.Where(post.Title==\"minmax-444\")";
        var expectedMax = await ef.Posts.Where(s => s.Title == "minmax-444").MaxAsync(s => s.CreatedAt);
        var expectedMin = await ef.Posts.Where(s => s.Title == "minmax-444").MinAsync(s => s.CreatedAt);

        // Act
        var max = await _handler.Handle($"{filter}.Max(CreatedAt)", new(), default);
        var min = await _handler.Handle($"{filter}.Min(CreatedAt)", new(), default);
        var distinct = await _handler.Handle($"{filter}.Select(Title).Distinct().ToList()", new(), default);

        // Assert
        max.Should().Be(expectedMax);
        min.Should().Be(expectedMin);
        (distinct as IEnumerable<string>)!.Should().ContainSingle().Which.Should().Be("minmax-444");
    }

    [IntegrationFact]
    public async Task Handle_SumAverageOnMetaIntField_TranslateOnPostgres()
    {
        // Arrange
        await _setupDataHelper.SetupPostTypeAndPosts(
            "sumavgType",
            [new() { Id = Guid.NewGuid(), Type = EMetaFieldType.Int, Key = "price", Title = "Price" }],
            3,
            (post, i) => post.Slug = $"sumavg-{i}",
            (post, i) => [new() { Type = EMetaFieldType.Int, Int = (i + 1) * 10 }]);

        // Act
        var sum = await _handler.Handle("sumavgType.Sum(price)", new(), default);
        var average = await _handler.Handle("sumavgType.Average(price)", new(), default);
        var max = await _handler.Handle("sumavgType.Max(price)", new(), default);

        // Assert
        sum.Should().Be(60);
        average.Should().Be(20d);
        max.Should().Be(30);
    }

    [IntegrationFact]
    public async Task Handle_DistinctBy_TranslatesViaRowNumber()
    {
        // Arrange — "db-555" уникальный маркер (БД общая на класс-фикстуру)
        var createdPosts = _fixture.CreateMany<PostEntity>(4).ToList();
        createdPosts.ForEach(s => s.Title = "db-555");
        (createdPosts[0].Slug, createdPosts[1].Slug) = ("db-a", "db-a");
        (createdPosts[2].Slug, createdPosts[3].Slug) = ("db-b", "db-b");
        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddRangeAsync(createdPosts);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        // Act
        var result = await _handler.Handle(
            "Posts.Where(post.Title==\"db-555\").DistinctBy(Slug).ToList()", new(), default);

        // Assert
        var rows = (result as IEnumerable<PostEntity>)!.ToList();
        rows.Should().HaveCount(2);
        rows.Select(s => s.Slug).Should().BeEquivalentTo(["db-a", "db-b"]);
    }

    [IntegrationFact]
    public async Task Handle_MaxByMinBy_TranslateOnPostgres()
    {
        // Arrange — "maxby-666" уникальный маркер (БД общая на класс-фикстуру)
        var now = DateTimeOffset.UtcNow;
        var createdPosts = _fixture.CreateMany<PostEntity>(3).ToList();
        for (int i = 0; i < createdPosts.Count; i++)
        {
            createdPosts[i].Title = "maxby-666";
            createdPosts[i].CreatedAt = now.AddHours(i);
        }
        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddRangeAsync(createdPosts);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        var filter = "Posts.Where(post.Title==\"maxby-666\")";
        var expectedMax = await ef.Posts.Where(s => s.Title == "maxby-666").OrderByDescending(s => s.CreatedAt).FirstAsync();
        var expectedMin = await ef.Posts.Where(s => s.Title == "maxby-666").OrderBy(s => s.CreatedAt).FirstAsync();

        // Act
        var maxBy = await _handler.Handle($"{filter}.MaxBy(CreatedAt)", new(), default) as PostEntity;
        var minBy = await _handler.Handle($"{filter}.MinBy(CreatedAt)", new(), default) as PostEntity;

        // Assert
        maxBy!.Id.Should().Be(expectedMax.Id);
        minBy!.Id.Should().Be(expectedMin.Id);
    }

    [IntegrationFact]
    public async Task Handle_LinqForMetaField_Works()
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
    public async Task Handle_LinqUnionOnDirectEntities_Works()
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
        var metaField = _fixture.Create<MetaFieldDto>() with { Key = "str1", Type = MetaFieldType.String };
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
