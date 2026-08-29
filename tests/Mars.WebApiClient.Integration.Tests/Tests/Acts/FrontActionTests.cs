using FluentAssertions;
using Mars.Contracts.XActions;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.XActions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Acts;

/// <summary>
/// XActionType.FrontAction: система видит команду в реестре, но хост её не исполняет.
/// </summary>
public class FrontActionTests : BaseWebApiClientTests
{
    public FrontActionTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task List_ShouldContainFrontActionCommand()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var list = await client.Act.List();

        //Assert
        var command = list.Should().ContainKey(FrontDemoXAction.CommandId).WhoseValue;
        command.Type.Should().Be(XActionType.FrontAction);
    }

    [IntegrationFact]
    public async Task Inject_FrontAction_OnHost_ReturnsError()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var result = await client.Act.Inject(FrontDemoXAction.CommandId);

        //Assert
        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("клиент");
    }
}
