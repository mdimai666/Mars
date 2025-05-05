using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;
using Mars.Controllers;

namespace Mars.WebApiClient.Integration.Tests.Tests.AppDebugs;

public class GetAppDebugTests : BaseWebApiClientTests
{
    public GetAppDebugTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    //[IntegrationFact]
    //public async Task GetLogs_Request_ShouldSuccess()
    //{
    //    //Arrange
    //    _ = nameof(AppDebugController.GetLogs);
    //    var client = GetWebApiClient();
    //    var logFileName = string.Format("app_{0:yyyy}-{0:MM}-{0:dd}.log", DateTime.Now);

    //    //Act
    //    var result = await client.AppDebug.GetLogs(logFileName);

    //    //Assert
    //    result.Ok.Should().BeTrue();
    //}

    [IntegrationFact]
    public async Task LogFiles_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(AppDebugController.LogFiles);
        var client = GetWebApiClient();

        //Act
        var result = await client.AppDebug.LogFiles();

        //Assert
        result.Count.Should().BeGreaterThanOrEqualTo(0);
    }
}
