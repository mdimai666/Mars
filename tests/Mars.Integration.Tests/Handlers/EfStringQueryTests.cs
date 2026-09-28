using AutoFixture;
using FluentAssertions;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.QueryLang.Host.Services;
using Mars.SiteEngine.Abstractions.Templators;
using Mars.Test.Common.FixtureCustomizes;

namespace Mars.Integration.Tests.Handlers;

public class EfStringQueryTests : ApplicationTests
{
    public EfStringQueryTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task Query_Where_Success()
    {
        // Arrange
        using var dbContext = AppFixture.MarsDbContext();
        var posts = dbContext.Posts;
        var ppt = new XInterpreter();
        await SetupData(dbContext);

        var efq = new EfStringQuery(posts, ppt);

        // Act
        var result = efq.Where("post.Title==\"111\"").ToList().Cast<PostEntity>().ToList();

        // Assert
        result.Count.Should().Be(2);
        result.Should().AllSatisfy(s => s.Title.Should().Be("111"));

        // bare-форма и lambda-форма дают тот же результат
        var bare = new EfStringQuery(posts, new XInterpreter())
            .Where("Title==\"111\"").ToList().Cast<PostEntity>().ToList();
        bare.Count.Should().Be(2);

        var lambda = new EfStringQuery(posts, new XInterpreter())
            .Where("p => p.Title==\"111\"").ToList().Cast<PostEntity>().ToList();
        lambda.Count.Should().Be(2);

        // Select: проекция становится текущим запросом
        var projected = (IQueryable)new EfStringQuery(posts, new XInterpreter())
            .Where("Title==\"111\"").Select("Title");
        projected.ElementType.Should().Be(typeof(string));
        var titles = projected.Cast<string>().ToList();
        titles.Count.Should().Be(2);
        titles.Should().OnlyContain(t => t == "111");

        // First/Last — терминальные: не сужают текущий запрос
        var q = new EfStringQuery(posts, new XInterpreter()).Where("Title==\"111\"");
        var first = (PostEntity)q.First()!;
        first.Title.Should().Be("111");
        q.ToList().Cast<PostEntity>().Count().Should().Be(2);

        // Last без детерминированной сортировки EF не транслирует — только с OrderBy
        var ordered = new EfStringQuery(posts, new XInterpreter()).Where("Title==\"111\"").OrderBy("Slug");
        var last = (PostEntity)ordered.Last()!;
        last.Title.Should().Be("111");
        ordered.ToList().Cast<PostEntity>().Count().Should().Be(2);
    }

    async Task SetupData(MarsDbContext dbContext)
    {
        var createdPosts = _fixture.CreateMany<PostEntity>(3).ToList();
        createdPosts.ForEach(s => s.Title = "111");
        createdPosts[0].Title = "000";
        await dbContext.Posts.AddRangeAsync(createdPosts);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
    }
}
