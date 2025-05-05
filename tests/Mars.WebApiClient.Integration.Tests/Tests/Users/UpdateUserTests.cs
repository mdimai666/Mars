using AutoFixture;
using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Users;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Users;

public sealed class UpdateUserTests : BaseWebApiClientTests
{
    GeneralUpdateTests<UserEntity, UpdateUserRequest, UserDetailResponse> _updateTest;

    public UpdateUserTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _updateTest = new(this, (client, req) => client.User.Update(req));

    }

    [IntegrationFact]
    public async Task UpdateUser_Request_Unauthorized()
    {
        await _updateTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task UpdateUser_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var entity = await CreateEntity<UserEntity>();
        var request = _fixture.Create<UpdateUserRequest>() with { Id = entity.Id };

        //Act
        var result = await client.User.Update(request);

        //Assert
        var dbEntity = await GetEntity<UserEntity>(entity.Id);
        dbEntity.Should().BeEquivalentTo(request, options => options
                    .ComparingRecordsByValue()
                    .ComparingByMembers<UpdateUserRequest>()
                    .Excluding(s => s.Roles)
                    .ExcludingMissingMembers());
    }

    [IntegrationFact]
    public void UpdateUser_InvalidModelRequest_ValidateError()
    {
        _updateTest.InvalidModelRequest_ValidateError(req => req with { FirstName = string.Empty }, "FirstName");
    }
}
