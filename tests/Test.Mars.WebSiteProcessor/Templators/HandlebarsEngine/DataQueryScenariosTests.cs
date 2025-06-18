using AutoFixture;
using FluentAssertions;
using Mars.Core.Extensions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.WebSite.Models;
using Mars.Host.Templators;
using Mars.Host.Templators.HandlebarsFunc;
using Mars.QueryLang.Host.Services;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http.Features;
using NSubstitute;

namespace Test.Mars.WebSiteProcessor.Templators.HandlebarsEngine;

public class DataQueryScenariosTests
{
    private readonly IFixture _fixture;
    private readonly IServiceProvider _serviceProvider;
    private readonly IFeatureCollection _featureCollection;
    private readonly IQueryLangProcessing _queryLangProcessing;
    private readonly IQueryLangLinqDatabaseQueryHandler _queryLangLinqDatabaseQueryHandler;

    public DataQueryScenariosTests()
    {
        _fixture = new Fixture();
        EntitiesCustomize.PostTypeDict = new Dictionary<string, PostTypeEntity> { ["post"] = new PostTypeEntity() };
        _fixture.Customize(new FixtureCustomize());
        _serviceProvider = Substitute.For<IServiceProvider>();
        _featureCollection = Substitute.For<IFeatureCollection>();
        _queryLangLinqDatabaseQueryHandler = Substitute.For<IQueryLangLinqDatabaseQueryHandler>();
        _queryLangProcessing = new QueryLangProcessing(new TemplatorFeaturesLocator(), _serviceProvider, _queryLangLinqDatabaseQueryHandler);
        _serviceProvider.GetService(typeof(IQueryLangProcessing)).Returns(_queryLangProcessing);
    }

    string Render(string html, Dictionary<string, object?>? data = null, Action<MyHandlebars>? builder = null)
    {
        using var hbs = new MyHandlebars();
        hbs.RegisterContextFunctions();
        builder?.Invoke(hbs);
        var template = hbs.Compile(html);
        var pageRenderContext = new PageRenderContext()
        {
            IsDevelopment = true,
            RenderParam = new(),
            SysOptions = new(),
            Request = new(new Uri("http://localhost")),
            User = null,
            TemplateContextVaribles = data ?? new()
        };
        var rctx = new HandlebarsHelperFunctionContext(pageRenderContext, _serviceProvider, CancellationToken.None);
        rctx.Features = _featureCollection;
        return template(pageRenderContext.TemplateContextVaribles, new { rctx });
    }

    [Fact]
    public void ListPostQuery_RenderList_ShouldListNames()
    {
        var template = """
            {{#context}}
            posts = ef.posts.Take(3)
            {{/context}}
            {{#each posts}}{{Title}}{{/each}}
            """;

        var posts = _fixture.CreateMany<PostEntity>(3).ToList();

        _queryLangLinqDatabaseQueryHandler.Handle("posts.Take(3)", Arg.Any<XInterpreter>(), default)
                                            .Returns(posts);

        var html = Render(template);

        html.Should().Contain(posts.Select(s => s.Title).JoinStr(""));
    }
}
