using Mars.Controllers;
using Mars.Host.Shared.Managers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;
using Mars.XActions;
using FluentAssertions;
using static Mars.Shared.Contracts.XActions.XActResult;

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
        var result = await client.Act.Inject(DummyAct.XAction.Id, []);

        //Assert
        result.Ok.Should().BeTrue();
    }

    [IntegrationFact]
    public void Inject_InvalidRequest_Fail404NotException()
    {
        //Arrange
        _ = nameof(ActController.Inject);
        _ = nameof(IActionManager.Inject);
        var client = GetWebApiClient();
        var invalidActionid = "ActX_invalidId";

        //Act
        var action = () => client.Act.Inject(invalidActionid, []);

        //Assert
        var result = action.Should().NotThrowAsync().RunSync().Subject;
        result.NextStep.Should().Be(XActionNextStep.Toast);
    }
}
