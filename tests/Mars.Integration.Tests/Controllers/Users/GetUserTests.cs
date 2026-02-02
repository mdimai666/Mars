using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Users;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.Users;

public class GetUserTests : ApplicationTests
{
    const string _apiUrl = "/api/User";

    public GetUserTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task GetUser_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(UserController.Get);
        _ = nameof(UserService.Get);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, Guid.NewGuid()).AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task GetUser_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserController.Get);
        _ = nameof(UserService.Get);
        var client = AppFixture.GetClient();

        var createdUser = await _fixture.AppendUserTypeMetaFieldsAndCreateUserWithMetaValues(AppFixture.DbFixture.DbContext);

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(createdUser.Id).GetJsonAsync<UserDetailResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetUser_InvalidModelRequest_Fail404()
    {
        //Arrange
        _ = nameof(UserController.Get);
        _ = nameof(UserService.Get);
        var client = AppFixture.GetClient();
        var invalidUserId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(invalidUserId)
                        .AllowAnyHttpStatus().SendAsync(HttpMethod.Get);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task ListUser_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(UserController.List);
        _ = nameof(UserService.List);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, "list/offset").AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task ListUser_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserController.List);
        _ = nameof(UserService.List);
        var client = AppFixture.GetClient();

        var createdUsers = _fixture.CreateMany<UserEntity>(3);

        var ef = AppFixture.MarsDbContext();
        await ef.Users.AddRangeAsync(createdUsers);
        await ef.SaveChangesAsync();

        var expectCount = ef.Users.Count();

        //Act
        var result = await client.Request(_apiUrl, "list/offset").GetJsonAsync<ListDataResult<UserListItemResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(expectCount);
    }

    [IntegrationFact]
    public async Task ListUser_SearchRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserController.List);
        _ = nameof(UserService.List);
        var client = AppFixture.GetClient();

        var searchString = $"{Guid.NewGuid()}";
        var searchNameString = $"nonUsePart_{searchString}";

        var createdUsers = _fixture.CreateMany<UserEntity>(3);
        createdUsers.ElementAt(0).FirstName = searchNameString;

        var ef = AppFixture.MarsDbContext();
        await ef.Users.AddRangeAsync(createdUsers);
        await ef.SaveChangesAsync();

        var expectUserId = createdUsers.ElementAt(0).Id;

        var request = new ListUserQueryRequest() { Search = searchString };

        //Act
        var result = await client.Request(_apiUrl, "list/offset")
                                    .AppendQueryParam(request)
                                    .GetJsonAsync<ListDataResult<UserListItemResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(1);
        result.Items.ElementAt(0).Id.Should().Be(expectUserId);
    }

    [IntegrationFact]
    public async Task GetEditModel_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserController.GetEditModel);
        _ = nameof(UserService.GetEditModel);
        var client = AppFixture.GetClient();

        var createdUser = await _fixture.AppendUserTypeMetaFieldsAndCreateUserWithMetaValues(AppFixture.DbFixture.DbContext);

        //Act
        var result = await client.Request(_apiUrl, "edit").AppendPathSegment(createdUser.Id).GetJsonAsync<UserEditViewModel>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetEditModelBlank_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserController.GetEditModelBlank);
        _ = nameof(UserService.GetEditModelBlank);
        var client = AppFixture.GetClient();

        var createdUser = await _fixture.AppendUserTypeMetaFieldsAndCreateUserWithMetaValues(AppFixture.DbFixture.DbContext);

        //Act
        var result = await client.Request(_apiUrl, "edit", "blank").AppendPathSegment("default").GetJsonAsync<UserEditViewModel>();

        //Assert
        result.Should().NotBeNull();
    }
}
