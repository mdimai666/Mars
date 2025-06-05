using AutoFixture;
using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Host.Shared.WebSite.Models;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.QueryLang.Host.Services;
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
}
