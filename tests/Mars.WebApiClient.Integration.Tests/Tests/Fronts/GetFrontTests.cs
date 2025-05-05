using Mars.Controllers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.WebApiClient.Interfaces;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Fronts;

public class GetFrontTests : BaseWebApiClientTests
{
    public GetFrontTests(ApplicationFixture appFixture) : base(appFixture)
    {

    }

    private async Task ActionShouldNotThrown<T>(Func<IMarsWebApiClient, Task<T>> action)
    {
        var act = () => action(GetWebApiClient());
        await act.Should().NotThrowAsync();
    }

    [IntegrationFact]
    public Task FrontMinimal_Request_Success()
    {
        _ = nameof(FrontController.FrontMinimal);
        _ = nameof(MarsWebApiClient.Front.FrontMinimal);
        return ActionShouldNotThrown(client => client.Front.FrontMinimal());
    }

    [IntegrationFact]
    public Task FrontFiles_Request_Success()
    {
        _ = nameof(FrontController.FrontFiles);
        _ = nameof(MarsWebApiClient.Front.FrontFiles);
        return ActionShouldNotThrown(client => client.Front.FrontFiles());
    }

    [IntegrationFact]
    public Task FrontSummaryInfo_Request_Success()
    {
        _ = nameof(FrontController.FrontSummaryInfo);
        _ = nameof(MarsWebApiClient.Front.FrontSummaryInfo);
        return ActionShouldNotThrown(client => client.Front.FrontSummaryInfo());
    }

    [IntegrationFact]
    public async Task GetPart_Request_Success()
    {
        //Arrange
        _ = nameof(FrontController.GetPart);
        _ = nameof(MarsWebApiClient.Front.GetPart);
        var client = GetWebApiClient();

        var files = await client.Front.FrontMinimal();

        //Act
        var result = await client.Front.GetPart(files.Pages.First().FileRelPath);

        //Assert
        result.Should().NotBeNull();
        result.Title.Should().NotBeNullOrEmpty();
    }

}
