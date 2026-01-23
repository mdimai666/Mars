using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Managers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.XActions;
using Mars.Test.Common.FixtureCustomizes;
using Mars.XActions;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.Acts;

/// <seealso cref="Mars.Controllers.ActController"/>
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
        var act = DummyAct.XAction; 
        string[] args = [];

        //Act
        var result = await client.Request(_apiUrl, "Inject", act.Id).PostJsonAsync(args).CatchUserActionError().ReceiveJson<XActResult>();

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
        var actId = "XAction_invalidId";
        string[] args = [];

        //Act
        var result = await client.Request(_apiUrl, "Inject", actId).AllowAnyHttpStatus().PostJsonAsync(args);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
