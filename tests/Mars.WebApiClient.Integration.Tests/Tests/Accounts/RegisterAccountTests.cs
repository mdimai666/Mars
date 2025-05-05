using FluentAssertions;
using Mars.Controllers;
using Mars.Core.Constants;
using Mars.Core.Utils;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Auth;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient.Integration.Tests.Tests.Accounts;

public class RegisterAccountTests : BaseWebApiClientTests
{
    public RegisterAccountTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task Register_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(AccountController.RegisterUser);
        SetupSelfRegistration(true);
        var client = GetWebApiClient(isAnonymous: true);
        var request = GetRegisterRequest();

        //Act
        var result = await client.Account.RegisterUser(request);

        //Assert
        result.IsSuccessfulRegistration.Should().BeTrue();
        result.Errors.Should().HaveCount(0);
        result.Code.Should().Be(StatusCodes.Status201Created);
        var user = await GetEntity<UserEntity>(s => s.Email == request.Email);
        user.FirstName.Should().Be(request.FirstName);
    }

    [IntegrationFact]
    public async Task Register_ValidRequestWhenRegisterDisallowed_ShouldFail()
    {
        //Arrange
        _ = nameof(AccountController.RegisterUser);
        SetupSelfRegistration(false);
        var client = GetWebApiClient(isAnonymous: true);
        var request = GetRegisterRequest();

        //Act
        var result = await client.Account.RegisterUser(request);

        //Assert
        result.IsSuccessfulRegistration.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Code.Should().Be(HttpConstants.UserActionErrorCode466);
    }

    [IntegrationFact]
    public async Task Register_BadRequest_ShouldFail()
    {
        //Arrange
        _ = nameof(AccountController.RegisterUser);
        SetupSelfRegistration(false);
        var client = GetWebApiClient(isAnonymous: true);
        var request = GetRegisterRequest() with { Email = "invalid" };

        //Act
        var result = await client.Account.RegisterUser(request);

        //Assert
        result.IsSuccessfulRegistration.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Code.Should().Be(HttpConstants.BadRequest400);
    }

    [IntegrationFact]
    public async Task Register_BadRequestEmptyFirstName_ShouldCreateFirstNameFromEmail()
    {
        //Arrange
        _ = nameof(AccountController.RegisterUser);
        SetupSelfRegistration(true);
        var client = GetWebApiClient(isAnonymous: true);
        var request = GetRegisterRequest() with { FirstName = null, LastName = null };

        //Act
        var result = await client.Account.RegisterUser(request);

        //Assert
        result.IsSuccessfulRegistration.Should().BeTrue();
        result.Errors.Should().HaveCount(0);
        result.Code.Should().Be(StatusCodes.Status201Created);

        var user = await GetEntity<UserEntity>(s => s.Email == request.Email);
        var expectFirstName = request.Email.Split('@', 2)[0];
        user.FirstName.Should().Be(expectFirstName);
    }

    private UserForRegistrationRequest GetRegisterRequest()
        => new()
        {
            Email = $"{Guid.NewGuid()}@example.com",
            Password = Password.Generate(10, 4),
            FirstName = "User1",
            LastName = null,
        };

    private void SetupSelfRegistration(bool allowUsersSelfRegister)
    {
        var optionService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();
        var sys = optionService.SysOption;
        sys.AllowUsersSelfRegister = allowUsersSelfRegister;
        optionService.SaveOption(optionService.SysOption);
    }
}
