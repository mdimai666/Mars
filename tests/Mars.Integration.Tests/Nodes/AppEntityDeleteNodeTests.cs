using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApp.Nodes.Host.Nodes;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Nodes;

public class AppEntityDeleteNodeTests : ApplicationTests
{
    const string _apiUrl = "/api2/AppEntityDeleteNode";
    private readonly INodeService _nodeService;

    public AppEntityDeleteNodeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _fixture.Customize(new MetaFieldDtoCustomize());
        _nodeService = AppFixture.ServiceProvider.GetRequiredService<INodeService>();
    }

    [IntegrationFact]
    public async Task Execute_DeletePostItems_ShouldReturnCode()
    {
        //Arrange
        _ = nameof(AppEntityReadNodeImpl.Execute);
        var client = AppFixture.GetClient();

        var forDeleteTag = "deleteTag";
        var createdPosts = _fixture.CreateMany<PostEntity>(4).ToList();
        createdPosts.ForEach(post => post.Tags = [forDeleteTag]);
        var ef = AppFixture.MarsDbContext();
        ef.Posts.AddRange(createdPosts);
        ef.SaveChanges();

        var expression = $"post.Where(post.Tags.Contains(\"{forDeleteTag}\")).ToList()";
        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(new HttpInNode { Method = "GET", UrlPattern = _apiUrl })
                                        .AddNext(new AppEntityReadNode { Expression = expression, ExpressionInput = NodeExpressionInput.String })
                                        .AddNext(new AppEntityDeleteNode())
                                        .AddNext(new HttpResponseNode())
                                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);

        //Act
        var result = await client.Request(_apiUrl).GetStringAsync();

        //Assert
        result.Should().NotBeNullOrEmpty();
        var deletedCountResponse = int.Parse(result);
        deletedCountResponse.Should().Be(createdPosts.Count);
    }
}
