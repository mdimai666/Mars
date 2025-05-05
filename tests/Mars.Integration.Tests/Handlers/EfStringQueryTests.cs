using AutoFixture;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Templators;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.QueryLang.Host.Services;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;

namespace Mars.Integration.Tests.Handlers;

public class EfStringQueryTests : ApplicationTests
{
    public EfStringQueryTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task Query_Where_Success()
    {
        // Arrange
        using var dbContext = AppFixture.MarsDbContext();
        var posts = dbContext.Posts;
        var ppt = new XInterpreter();
        await SetupData(dbContext);

        var efq = new EfStringQuery<PostEntity>(posts, ppt);

        // Act
        var result = efq.Where("post.Title==\"111\"").ToList();

        // Assert
        result.Count.Should().Be(2);
        result.Should().AllSatisfy(s => s.Title.Should().Be("111"));
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
