using FluentAssertions;
using Mars.Core.Exceptions;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.XActions;
using Mars.Test.Common.FixtureCustomizes;
using Mars.XActions;
using Mars.XActions.ContentRecipes;

namespace Mars.WebApiClient.Integration.Tests.Tests.Acts;

/// <summary>
/// GET /api/Act/list — список команд для UI: схема аргументов, фильтр системных;
/// GET /api/Act/options — динамические варианты выбора.
/// </summary>
public class ListActTests : BaseWebApiClientTests
{
    public ListActTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task List_ShouldReturnCommandsWithArgumentSchema()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var list = await client.Act.List();

        //Assert
        list.Should().ContainKey(ClearCacheAct.CommandId);

        var templateCommand = list.Should().ContainKey(CreatePostTypePresentationTemplateAct.CommandId).WhoseValue;
        templateCommand.Arguments.Should().NotBeNull();
        templateCommand.Arguments!.Single(a => a.Name == CreatePostTypePresentationTemplateAct.PostTypeNameArg)
                       .Required.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task List_ShouldDeclareDynamicOptionsSource()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var list = await client.Act.List();

        //Assert
        var mockPostsCommand = list.Should().ContainKey(CreateMockPostsAct.CommandId).WhoseValue;
        var postTypeArgument = mockPostsCommand.Arguments!.Single(a => a.Name == CreateMockPostsAct.PostTypeArg);

        postTypeArgument.Type.Should().Be(XActionArgumentType.Choice);
        postTypeArgument.DefaultValue.Should().Be("post");
        postTypeArgument.OptionsSource.Should().Be(CreateMockPostsAct.PostTypesOptionsSource);
    }

    [IntegrationFact]
    public async Task List_ShouldDeclareStaticOptions()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var list = await client.Act.List();

        //Assert
        var formTestCommand = list.Should().ContainKey(FormTestAct.CommandId).WhoseValue;
        var choiceArgument = formTestCommand.Arguments!.Single(a => a.Name == FormTestAct.ChoiceArg);

        choiceArgument.Options.Should().NotBeNull();
        choiceArgument.Options!.Should().Contain(o => o.Key == "one" && o.Label == "Первый");
    }

    [IntegrationFact]
    public async Task List_ShouldHideSystemCommands()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var list = await client.Act.List();

        //Assert
        list.Should().NotContainKey(DummyAct.CommandId);
    }

    [IntegrationFact]
    public async Task Options_ShouldReturnDynamicOptions()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var options = await client.Act.Options(CreateMockPostsAct.PostTypesOptionsSource);

        //Assert
        options.Should().Contain(o => o.Key == "post");
    }

    [IntegrationFact]
    public async Task Options_UnknownSource_Fail404Exception()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var action = () => client.Act.Options("unknown.options.source");

        //Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }
}
