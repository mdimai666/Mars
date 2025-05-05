using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Repositories;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Options.Models;
using Mars.Shared.Options;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Options;

/// <seealso cref="Mars.Controllers.OptionController"/>
public class UpdateOptionTests : ApplicationTests
{
    const string _apiUrl = "/api/Option";
    private readonly IOptionService _optionService;
    private readonly IOptionRepository _optionRepository;

    public UpdateOptionTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _optionService = appFixture.ServiceProvider.GetRequiredService<IOptionService>();
        _optionRepository = appFixture.ServiceProvider.GetRequiredService<IOptionRepository>();
    }

    [IntegrationFact]
    public async Task UpdateOption_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(OptionController.SaveOption);
        _ = nameof(OptionRepository.Update);
        var client = AppFixture.GetClient(true);
        var opt = typeof(MaintenanceModeOption);
        var optionValue = _optionService.GetOption<MaintenanceModeOption>();
        optionValue.MaintenanceStaticPageTitle = Guid.NewGuid().ToString();

        //Act
        var result = await client.Request(_apiUrl, "Option", opt.Name).AllowAnyHttpStatus().PutJsonAsync(optionValue);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task UpdateOption_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(OptionController.SaveOption);
        _ = nameof(OptionRepository.Update);
        var client = AppFixture.GetClient();
        var opt = typeof(MaintenanceModeOption);
        var optionValue = _optionService.GetOption<MaintenanceModeOption>();
        optionValue.MaintenanceStaticPageTitle = Guid.NewGuid().ToString();

        //Act
        var res = await client.Request(_apiUrl, "Option", opt.Name).PutJsonAsync(optionValue).CatchUserActionError();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);

        var dbOption = await _optionRepository.GetKey<MaintenanceModeOption>(opt.Name);
        dbOption.Should().NotBeNull();

        dbOption.Should().BeEquivalentTo(optionValue, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<MaintenanceModeOption>()
            .ExcludingMissingMembers());
    }

    [IntegrationFact]
    public async Task SaveSysOptions_AnonimRequest_Unauthorized()
    {
        //Arrange
        _ = nameof(OptionController.SaveSysOptions);
        _ = nameof(OptionRepository.Update);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, "SysOptions").AllowAnyHttpStatus().PutJsonAsync(new SysOptions());

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

#if false
    [IntegrationFact]
    public async Task UpdateOption_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(OptionController.SaveOption);
        _ = nameof(OptionService.SaveOption);
        var client = AppFixture.GetClient();
        var opt = typeof(MaintenanceModeOption);
        var optionValue = _optionService.GetOption<MaintenanceModeOption>();
        optionValue.MaintenanceStaticPageTitle = string.Empty;

        //Act
        var result = await client.Request(_apiUrl, "Option", opt.Name).PutJsonAsync(optionValue).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.ValidateSatisfy(new()
        {
            [nameof(MaintenanceModeOption.Title)] = ["*Title*required*"],
        });
    } 
#endif

}
