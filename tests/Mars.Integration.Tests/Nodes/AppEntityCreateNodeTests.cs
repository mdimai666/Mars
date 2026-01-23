using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Host.Shared.HttpModule;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApp.Nodes.Front.Models.AppEntityForms;
using Mars.WebApp.Nodes.Host.Builders;
using Mars.WebApp.Nodes.Host.Nodes;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Nodes;

public class AppEntityCreateNodeTests : ApplicationTests, IAsyncLifetime
{
    const string _apiUrl = "/api2/AppEntityCreateNode";
    private readonly INodeService _nodeService;
    private readonly IAppEntityFormBuilderFactory _factory;
    private readonly IPostJsonService _postJsonService;
    private readonly CreatePostTypeQuery _postType;
    private const string EntityName = "Post";
    private const string SubPostTypeName = "exPost";

    public AppEntityCreateNodeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _fixture.Customize(new MetaFieldDtoCustomize());
        _nodeService = AppFixture.ServiceProvider.GetRequiredService<INodeService>();
        _factory = AppFixture.ServiceProvider.GetRequiredService<IAppEntityFormBuilderFactory>();
        _postJsonService = AppFixture.ServiceProvider.GetRequiredService<IPostJsonService>();

        _postType = new CreatePostTypeQuery
        {
            Title = "ExPost",
            TypeName = SubPostTypeName,
            PostContentSettings = new() { PostContentType = PostTypeConstants.DefaultPostContentTypes.PlainText, CodeLang = null },
            EnabledFeatures = [PostTypeConstants.Features.Content],
            Tags = [],
            Disabled = false,
            PostStatusList = [],
            MetaFields = [
                _fixture.Create<MetaFieldDto>() with { Type = MetaFieldType.String, Key = "str1" },
                _fixture.Create<MetaFieldDto>() with { Type = MetaFieldType.Int, Key = "int1" },
            ],
        };
    }

    public async Task InitializeAsync()
    {
        var pts = AppFixture.ServiceProvider.GetRequiredService<IPostTypeService>();
        await pts.Create(_postType, default);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [IntegrationFact]
    public async Task Execute_CreatePostFromFormLiterallyFields_ShouldCreateEntity()
    {
        //Arrange
        _ = nameof(AppEntityCreateNodeImpl.Execute);
        _ = nameof(AppEntityFormBuilderFactory);
        _ = nameof(PostEntityCreateFormBuilder.Save);
        var client = AppFixture.GetClient();

        //var expression = "Posts.Where(post.Title!=\"111\").ToList()";

        var entityUri = $"/{EntityName}/{SubPostTypeName}";
        var builder = _factory.GetBuilder(EntityName);
        var formsDict = new AppEntityCreateFormsBuilderDictionary { Forms = [builder.CreateForm(entityUri)] };
        var postId = Guid.NewGuid();

        var model = new AppEntityCreateFormSchemaEditModel(formsDict, new()
        {
            EntityUri = entityUri,
            PropertyBindings = [
                new(){ PropertyName = nameof(CreatePostQuery.Id), ValueOrExpression = postId.ToString(), IsEvalExpression = false },
                new(){ PropertyName = nameof(CreatePostQuery.Title), ValueOrExpression = _fixture.Create("Title"), IsEvalExpression = false },
                new(){ PropertyName = nameof(CreatePostQuery.Content), ValueOrExpression = "X", IsEvalExpression = false },
                new(){ PropertyName = "meta.str1", ValueOrExpression = "string value 1", IsEvalExpression = false },
                new(){ PropertyName = "meta.int1", ValueOrExpression = "123", IsEvalExpression = false },
            ]
        });

        var formData = model.ToRequest();

        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(new HttpInNode { Method = "GET", UrlPattern = _apiUrl })
                                        .AddNext(new AppEntityCreateNode { FormCommand = formData })
                                        .AddNext(new HttpResponseNode())
                                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);

        //Act
        var result = await client.Request(_apiUrl).GetStringAsync();

        //Assert
        result.Should().NotBeNullOrEmpty();
        var createdPost = await _postJsonService.GetDetail(postId, renderContent: false);
        createdPost.Should().NotBeNull();
        var propertiesDict = model.PropertyBindings.ToDictionary(s => s.PropertyName);
        createdPost.Title.Should().BeEquivalentTo(propertiesDict["Title"].ValueOrExpression);
        createdPost.Content.Should().BeEquivalentTo(propertiesDict["Content"].ValueOrExpression);
        createdPost.Meta["str1"].ToString().Should().BeEquivalentTo(propertiesDict["meta.str1"].ValueOrExpression);
        ((int)createdPost.Meta["int1"]!).Should().Be(int.Parse(propertiesDict["meta.int1"].ValueOrExpression));
    }

    [IntegrationFact]
    public async Task Execute_CreatePostFromFormAsExpressionAndAuto_ShouldCreateEntity()
    {
        //Arrange
        _ = nameof(AppEntityCreateNodeImpl.Execute);
        _ = nameof(AppEntityFormBuilderFactory);
        _ = nameof(PostEntityCreateFormBuilder.Save);
        _ = nameof(HttpInNodeHttpRequestContext.Request);
        var client = AppFixture.GetClient();

        //var expression = "Posts.Where(post.Title!=\"111\").ToList()";

        var entityUri = $"/{EntityName}/{SubPostTypeName}";
        var builder = _factory.GetBuilder(EntityName);
        var formsDict = new AppEntityCreateFormsBuilderDictionary { Forms = [builder.CreateForm(entityUri)] };
        var postId = Guid.NewGuid();
        var bodyContent = _fixture.Create("Body");

        var model = new AppEntityCreateFormSchemaEditModel(formsDict, new()
        {
            EntityUri = entityUri,
            PropertyBindings = [
                new(){ PropertyName = nameof(CreatePostQuery.Id), ValueOrExpression = $"new Guid(\"{postId}\")", IsEvalExpression = true },
                new(){ PropertyName = nameof(CreatePostQuery.Slug), ValueOrExpression = "" /*auto*/, IsEvalExpression = true },
                new(){ PropertyName = nameof(CreatePostQuery.Title), ValueOrExpression = "HttpInNodeHttpRequestContext.Request.Path", IsEvalExpression = true },
                new(){ PropertyName = nameof(CreatePostQuery.Content), ValueOrExpression = "Payload", IsEvalExpression = true },
                new(){ PropertyName = "meta.str1", ValueOrExpression = "Payload+1", IsEvalExpression = true },
                new(){ PropertyName = "meta.int1", ValueOrExpression = "2+2", IsEvalExpression = true },
            ]
        });

        var formData = model.ToRequest();

        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(new HttpInNode { Method = "POST", UrlPattern = _apiUrl })
                                        .AddNext(new AppEntityCreateNode { FormCommand = formData })
                                        .AddNext(new HttpResponseNode())
                                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);

        //Act
        var result = await client.Request(_apiUrl).PostStringAsync(bodyContent).ReceiveString();

        //Assert
        result.Should().NotBeNullOrEmpty();
        var createdPost = await _postJsonService.GetDetail(postId, renderContent: false);
        createdPost.Should().NotBeNull();
        var propertiesDict = model.PropertyBindings.ToDictionary(s => s.PropertyName);
        createdPost.Title.Should().BeEquivalentTo(_apiUrl);
        createdPost.Content.Should().BeEquivalentTo(bodyContent);
        createdPost.Meta["str1"].ToString().Should().BeEquivalentTo(bodyContent + "1");
        ((int)createdPost.Meta["int1"]!).Should().Be(4);
    }

}
