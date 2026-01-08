using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.TestControllers;
using FluentAssertions;
using Flurl.Http;

namespace Mars.Integration.Tests.Services;

public sealed class DataTypesJsonResponseTests : ApplicationTests
{
    const string _apiUrl = "/api-test/TestApi1/";

    public DataTypesJsonResponseTests(ApplicationFixture appFixture) : base(appFixture)
    {

    }

    [IntegrationFact]
    public async Task TimeOnlyResponse_ReturnValidString_Success()
    {
        //Arrange
        _ = nameof(TestApi1Controller.TimeOnlyResponse);
        var client = AppFixture.GetClient();
        var expectResult = "\"08:12:16\"";

        //Act
        var result = await client.Request(_apiUrl, "TimeOnlyResponse").GetStringAsync();

        //Assert
        result.Should().Be(expectResult);
    }

    [IntegrationFact]
    public async Task TimeOnlyResponse_ReturnValid_Success()
    {
        //Arrange
        _ = nameof(TestApi1Controller.TimeOnlyResponse);
        var client = AppFixture.GetClient();
        var expectResult = new TimeOnly(8, 12, 16);

        //Act
        var result = await client.Request(_apiUrl, "TimeOnlyResponse").GetJsonAsync<TimeOnly>();

        //Assert
        result.Should().Be(expectResult);
    }

    [IntegrationFact]
    public async Task TimeOnlyResponse_ReturnNullable_Success()
    {
        //Arrange
        _ = nameof(TestApi1Controller.TimeOnlyResponse);
        var client = AppFixture.GetClient();
        var expectResult = new TimeOnly(8, 12, 16);

        //Act
        var result = await client.Request(_apiUrl, "TimeOnlyResponse").GetJsonAsync<TimeOnly?>();

        //Assert
        result.Should().Be(expectResult);
    }

    [IntegrationFact]
    public async Task DateOnlyResponse_ReturnValidString_Success()
    {
        //Arrange
        _ = nameof(TestApi1Controller.DateOnlyResponse);
        var client = AppFixture.GetClient();
        var expectResult = "\"2022-06-22\"";

        //Act
        var result = await client.Request(_apiUrl, "DateOnlyResponse").GetStringAsync();

        //Assert
        result.Should().Be(expectResult);
    }

    [IntegrationFact]
    public async Task DateOnlyResponse_ReturnValid_Success()
    {
        //Arrange
        _ = nameof(TestApi1Controller.DateOnlyResponse);
        var client = AppFixture.GetClient();
        var expectResult = new DateOnly(2022, 06, 22);

        //Act
        var result = await client.Request(_apiUrl, "DateOnlyResponse").GetJsonAsync<DateOnly>();

        //Assert
        result.Should().Be(expectResult);
    }

    [IntegrationFact]
    public async Task DateOnlyResponse_ReturnNullable_Success()
    {
        //Arrange
        _ = nameof(TestApi1Controller.DateOnlyResponse);
        var client = AppFixture.GetClient();
        var expectResult = new DateOnly(2022, 06, 22);

        //Act
        var result = await client.Request(_apiUrl, "DateOnlyResponse").GetJsonAsync<DateOnly?>();

        //Assert
        result.Should().Be(expectResult);
    }
}
