using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Contracts.XActions;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Server.Controllers;
using Mars.Server.Managers;
using Mars.Test.Common.FixtureCustomizes;
using Mars.XActions;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.Acts;

/// <seealso cref="Mars.Server.Controllers.ActController"/>
public class InjectActTests : ApplicationTests
{
    const string _apiUrl = "/api/Act";

    public InjectActTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

#if DEBUG
    [IntegrationFact]
    public async Task Inject_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(ActController.Inject);
        _ = nameof(XActionManager.Inject);
        _ = nameof(DummyAct);
        var client = AppFixture.GetClient();
        var call = new XActionCommandCall { Id = DummyAct.CommandId };

        //Act
        var result = await client.Request(_apiUrl, "Inject").PostJsonAsync(call).CatchUserActionError().ReceiveJson<XActResult>();

        //Assert
        result.Should().NotBeNull();
        result.Ok.Should().BeTrue();
        result.Message.Should().Match("act executed.*");
    }
#endif

    [IntegrationFact]
    public async Task Inject_InvalidId_FailNotFound404()
    {
        //Arrange
        _ = nameof(ActController.Inject);
        _ = nameof(XActionManager.Inject);
        var client = AppFixture.GetClient();
        var call = new XActionCommandCall { Id = "XAction_invalidId" };

        //Act
        var result = await client.Request(_apiUrl, "Inject").AllowAnyHttpStatus().PostJsonAsync(call);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
