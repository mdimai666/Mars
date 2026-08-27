using FluentAssertions;
using Mars.Controllers;
using Mars.Core.Exceptions;
using Mars.Server.Abstractions.Managers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Contracts.XActions;
using Mars.Test.Common.FixtureCustomizes;
using Mars.XActions;
using Mars.XActions.ContentRecipes;

namespace Mars.WebApiClient.Integration.Tests.Tests.Acts;

public class InjectActTests : BaseWebApiClientTests
{
    public InjectActTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task Inject_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(ActController.Inject);
        _ = nameof(IActionManager.Inject);
        var client = GetWebApiClient();

        //Act
        var result = await client.Act.Inject(DummyAct.CommandId);

        //Assert
        result.Ok.Should().BeTrue();
        result.Effects.Should().ContainEquivalentOf(new NavigateEffect("/dev"));
        result.Effects.OfType<TriggerEventEffect>().Should().Contain(e => e.Name == "dummy-act-executed");
    }

    [IntegrationFact]
    public async Task Inject_InvalidRequest_Fail404Exception()
    {
        //Arrange
        _ = nameof(ActController.Inject);
        _ = nameof(IActionManager.Inject);
        var client = GetWebApiClient();
        var invalidActionid = "ActX_invalidId";

        //Act
        var action = () => client.Act.Inject(invalidActionid);

        //Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [IntegrationFact]
    public async Task Inject_NamedArgs_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var postTypeName = $"xactp1{Guid.NewGuid():N}";

        //Act
        var result = await client.Act.Inject(
            CreatePostTypePresentationTemplateAct.CommandId,
            new Dictionary<string, string>
            {
                [CreatePostTypePresentationTemplateAct.PostTypeNameArg] = postTypeName,
            });

        //Assert
        result.Ok.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task Inject_MissingRequiredArg_ShouldFailResult()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var result = await client.Act.Inject(CreatePostTypePresentationTemplateAct.CommandId);

        //Assert
        result.Ok.Should().BeFalse();
        result.Message.Should().Contain(CreatePostTypePresentationTemplateAct.PostTypeNameArg);
    }

    [IntegrationFact]
    public async Task Inject_LinkCommand_ShouldReturnWarningResult()
    {
        //Arrange
        var client = GetWebApiClient();
        var linkId = nameof(GenSourceCodeController.MetaTypesSourceCode) + "+csharp";

        //Act
        var result = await client.Act.Inject(linkId);

        //Assert
        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("ссылка");
    }
}
