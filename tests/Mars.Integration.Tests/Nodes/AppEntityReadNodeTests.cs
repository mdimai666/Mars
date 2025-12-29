using FluentAssertions;
using Flurl.Http;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Host.Services;
using Mars.QueryLang;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApp.Nodes.Host.Nodes;
using Mars.WebApp.Nodes.Models.NodeEntityQuery;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Nodes;

public class AppEntityReadNodeTests : ApplicationTests
{
    const string _apiUrl = "/api2/AppEntityReadNode";
    private readonly INodeService _nodeService;

    public AppEntityReadNodeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _fixture.Customize(new MetaFieldDtoCustomize());
        _nodeService = AppFixture.ServiceProvider.GetRequiredService<INodeService>();
    }

    [IntegrationFact]
    public async Task Execute_WithExpressionString_ShouldReturnCode()
    {
        //Arrange
        _ = nameof(AppEntityReadNodeImpl.Execute);
        var client = AppFixture.GetClient();

        var expression = "Posts.Where(post.Title!=\"111\").ToList()";
        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(new HttpInNode { Method = "GET", UrlPattern = _apiUrl })
                                        .AddNext(new AppEntityReadNode { Expression = expression, ExpressionInput = NodeExpressionInput.String })
                                        .AddNext(new HttpResponseNode())
                                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);

        //Act
        var result = await client.Request(_apiUrl).GetStringAsync();

        //Assert
        result.Should().NotBeNullOrEmpty();
    }

    [IntegrationFact]
    public async Task Execute_WithNodeEntityQueryBuilder_ShouldReturnCode()
    {
        //Arrange
        _ = nameof(AppEntityReadNodeImpl.Execute);
        var client = AppFixture.GetClient();

        //var expression = "Posts.Where(post.Title!=\"111\").ToList()";

        var metaModelTypesLocator = AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>();
        var queryLangHelperAvailableMethods = AppFixture.ServiceProvider.GetRequiredService<IQueryLangHelperAvailableMethodsProvider>();
        var databaseEntityTypeCatalogService = AppFixture.ServiceProvider.GetRequiredService<IDatabaseEntityTypeCatalogService>();

        var builder = new NodeEntityQueryBuilder(metaModelTypesLocator, queryLangHelperAvailableMethods, databaseEntityTypeCatalogService);

        var formDict = builder.CreateDictionary();

        var entityReadQuery = new NodeEntityQueryRequestModel
        {
            EntityName = "Posts", // Post.post
            CallChains = [
                new() { MethodName = "Where", Parameters = ["post.Title!=\"111\""] },
                new() { MethodName = "ToList", },
            ]
        };

        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(new HttpInNode { Method = "GET", UrlPattern = _apiUrl })
                                        .AddNext(new AppEntityReadNode { Query = entityReadQuery, ExpressionInput = NodeExpressionInput.Builder })
                                        .AddNext(new HttpResponseNode())
                                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);

        //Act
        var result = await client.Request(_apiUrl).GetStringAsync();

        //Assert
        result.Should().NotBeNullOrEmpty();
    }
}
