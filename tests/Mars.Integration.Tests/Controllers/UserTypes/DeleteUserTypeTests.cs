using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.UserTypes;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.UserTypes;

/// <seealso cref="UserTypeController.Delete(Guid, CancellationToken)"/>
public class DeleteUserTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/UserType";

    public DeleteUserTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task DeleteUserType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserTypeController.Delete);
        var client = AppFixture.GetClient();

        var postType = _fixture.Create<UserTypeEntity>();
        var ef = AppFixture.MarsDbContext();
        await ef.UserTypes.AddAsync(postType);
        await ef.SaveChangesAsync();
        var deletingId = postType.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var postTypeEntity = ef.UserTypes.FirstOrDefault(s => s.Id == deletingId);
        postTypeEntity.Should().BeNull();
    }

    [IntegrationFact]
    public async Task DeleteUserType_InvalidId_404NotFound()
    {
        //Arrange
        _ = nameof(UserTypeController.Delete);
        var client = AppFixture.GetClient();

        var deletingId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task DeleteUserType_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(UserTypeController.Delete);
        _ = nameof(UserTypeService.Delete);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, Guid.NewGuid()).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task DeleteUserType_TryDeleteDefaultUserType_ShouldValidationError()
    {
        //Arrange
        _ = nameof(UserTypeController.Delete);
        _ = nameof(DeleteUserTypeQueryValidator);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var defaultUserType = ef.UserTypes.First(s => s.TypeName == "default");

        //Act
        var validate = await client.Request(_apiUrl).AppendPathSegment(defaultUserType.Id).DeleteAsync().ReceiveValidationError();

        //Assert
        validate.Errors.Should().HaveCount(1);
        validate.Errors.ElementAt(0).Key.Should().Be(nameof(UserTypeSummary.TypeName));
        validate.Errors[nameof(UserTypeSummary.TypeName)].First().Should().Match("*internal type and cannot be delete");

        ef.UserTypes.Any(s => s.Id == defaultUserType.Id).Should().BeTrue();
    }

    [IntegrationFact]
    public async Task DeleteManyUserType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserTypeController.DeleteMany);
        var client = AppFixture.GetClient();

        var userTypes = _fixture.CreateMany<UserTypeEntity>().ToList();
        var ef = AppFixture.MarsDbContext();
        await ef.UserTypes.AddRangeAsync(userTypes);
        await ef.SaveChangesAsync();
        var deletingIds = userTypes.Select(s => s.Id).ToList();

        //Act
        var result = await client.Request(_apiUrl, "DeleteMany").AppendQueryParam(new { ids = deletingIds }).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var userTypeEntity = ef.UserTypes.Any(s => deletingIds.Contains(s.Id));
        userTypeEntity.Should().BeFalse();
    }
}
