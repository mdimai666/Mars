using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Services;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Options.Models;
using Mars.Shared.Options;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Options;

public class GetOptionTests : ApplicationTests
{
    const string _apiUrl = "/api/Option";
    private readonly IOptionService _optionService;

    public GetOptionTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _optionService = appFixture.ServiceProvider.GetRequiredService<IOptionService>();
    }

    [IntegrationFact]
    public async Task GetOption_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(OptionController.GetOption);
        _ = nameof(OptionService.GetOptionByClass);
        var client = AppFixture.GetClient(true);
        var opt = typeof(ApiOption);

        //Act
        var result = await client.Request(_apiUrl, "Option", opt.Name).AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task GetOption_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(OptionController.GetOption);
        _ = nameof(OptionService.GetOptionByClass);
        var client = AppFixture.GetClient();
        var opt = typeof(ApiOption);
        var optionValue = _optionService.GetOption(opt);

        //Act
        var result = await client.Request(_apiUrl, "Option", opt.Name).GetJsonAsync<ApiOption>();

        //Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(optionValue, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<ApiOption>()
            .ExcludingMissingMembers());
    }

    [IntegrationFact]
    public async Task GetOption_InvalidModelRequest_Fail404()
    {
        //Arrange
        _ = nameof(OptionController.GetOption);
        _ = nameof(OptionService.GetOptionByClass);
        var client = AppFixture.GetClient();
        var invalidOptionClassName = "notValidOptionClassName";

        //Act
        var result = await client.Request(_apiUrl, "Option", invalidOptionClassName)
                        .AllowAnyHttpStatus().SendAsync(HttpMethod.Get);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task GetSysOptions_AnonimRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(OptionController.GetSysOptions);
        _ = nameof(OptionService.SysOption);
        var client = AppFixture.GetClient(true);
        var opt = typeof(SysOptions);
        var optionValue = _optionService.SysOption;

        //Act
        var result = await client.Request(_apiUrl, opt.Name).GetJsonAsync<SysOptions>();

        //Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(optionValue, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<SysOptions>()
            .ExcludingMissingMembers());
    }
}
