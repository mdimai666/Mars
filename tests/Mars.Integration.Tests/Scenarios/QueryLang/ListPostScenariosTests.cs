using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Scenarios.QueryLang;

public class ListPostScenariosTests : ApplicationTests
{
    private readonly SetupDataHelper _setupDataHelper;
    private readonly IQueryLangLinqDatabaseQueryHandler _queryLang;

    public ListPostScenariosTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _setupDataHelper = new SetupDataHelper(appFixture);
        _queryLang = AppFixture.ServiceProvider.GetRequiredService<IQueryLangLinqDatabaseQueryHandler>();
    }

    [IntegrationFact]
    public async Task Scenario_PinnedPostsPrepareUsingUnion_Success()
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

        using var ef = AppFixture.DbFixture.DbContext;
        var expectPosts = ef.Posts.Where(s => s.PostType.TypeName == postTypeName);

        /*
            sticked = news.OrderByDescending(Created).Where(post.sticked==true).Take(1).Fill()
            news = news.OrderByDescending(Created).Take(40).Fill()
            new_Items = [..sticked, ..news]
         */

        // не будет работать из-за ef. При .Union нельзая использовать .Select предварительно, а оно используется для myType
        //var expression = "myType.OrderByDescending(Slug).Union(myType.Where(post.pinned==true).Take(1)).ToList()";
        var expression = "Union(myType.Where(post.pinned==true).Take(1), myType.OrderByDescending(Slug))";

        var unionMethodPosts = await expectPosts.OrderByDescending(s => s.Slug)
                                        .Union(expectPosts.Where(s => s.Slug == "post-1").Take(1))
                                        .ToListAsync();

        // Act
        var result = await _queryLang.Handle(expression, new(), default);
        var t = result.GetType();
        var expPosts = (result as IEnumerable<object>)!.Cast<PostEntity>();

        // Assert
        unionMethodPosts.Select(s => s.Slug).Should().BeEquivalentTo(expectOrder);
        expPosts.Select(s => s.Slug).Should().BeEquivalentTo(expectOrder);
    }

}
