using Mars.Core.Exceptions;
using Mars.Host.Shared.Repositories;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Options.Models;
using Mars.Shared.Options;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient.Integration.Tests.Tests.Options;

public sealed class UpdateOptionTests : BaseWebApiClientTests
{
    private readonly IOptionRepository _optionRepository;

    public UpdateOptionTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _optionRepository = appFixture.ServiceProvider.GetRequiredService<IOptionRepository>();
    }

    [IntegrationFact]
    public async Task UpdateOption_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);

        //Act
        var action = () => client.Option.GetOption<ApiOption>();

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task UpdateOption_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var request = new MaintenanceModeOption()
        {
            MaintenanceStaticPageTitle = Guid.NewGuid().ToString(),
        };

        //Act
        var action = () => client.Option.SaveOption(request);

        //Assert
        await action.Should().NotThrowAsync();
        var dbEntity = _optionRepository.GetKey<MaintenanceModeOption>(typeof(MaintenanceModeOption).Name);
        dbEntity.Should().BeEquivalentTo(request, options => options
                    .ComparingRecordsByValue()
                    .ExcludingMissingMembers());
    }

    [IntegrationFact]
    public async Task SaveSysOptions_AnonimRequest_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);
        var optionValue = new SysOptions();

        //Act
        var action = () => client.Option.SaveSysOptions(optionValue);

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

#if false
    [IntegrationFact]
    public void UpdateOption_InvalidModelRequest_ValidateError()
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<UpdatePostTypeRequest>();
        request = request with
        {
            Title = string.Empty
        };

        //Act
        var action = () => client.Option.SaveOption(request);

        //Assert
        action.Should().ThrowAsync<MarsValidationException>().RunSync()
            .And.Errors.Should().ContainKey("Title");
    } 
#endif
}
