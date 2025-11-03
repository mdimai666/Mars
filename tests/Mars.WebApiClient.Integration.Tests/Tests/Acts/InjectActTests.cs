using FluentAssertions;
using Mars.Controllers;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Managers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Mars.XActions;

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
    public async Task Inject_InvalidRequest_Fail404Exception()
    {
        //Arrange
        _ = nameof(ActController.Inject);
        _ = nameof(IActionManager.Inject);
        var client = GetWebApiClient();
        var invalidActionid = "ActX_invalidId";

        //Act
        var action = () => client.Act.Inject(invalidActionid, []);

        //Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }
}
