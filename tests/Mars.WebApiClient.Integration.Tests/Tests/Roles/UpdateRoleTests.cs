using AutoFixture;
using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Roles;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Roles;

public sealed class UpdateRoleTests : BaseWebApiClientTests
{
    GeneralUpdateTests<RoleEntity, UpdateRoleRequest, RoleDetailResponse> _updateTest;

    public UpdateRoleTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _updateTest = new(this, (client, req) => client.Role.Update(req));

    }

    [IntegrationFact]
    public async Task UpdateRole_Request_Unauthorized()
    {
        await _updateTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task UpdateRole_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var entity = await CreateEntity<RoleEntity>();
        var request = _fixture.Create<UpdateRoleRequest>() with { Id = entity.Id };

        //Act
        var result = await client.Role.Update(request);

        //Assert
        var dbEntity = await GetEntity<RoleEntity>(entity.Id);
        dbEntity.Should().BeEquivalentTo(request, options => options
                    .ComparingRecordsByValue()
                    .ComparingByMembers<UpdateRoleRequest>()
                    .ExcludingMissingMembers());
    }

    [IntegrationFact]
    public void UpdateRole_InvalidModelRequest_ValidateError()
    {
        _updateTest.InvalidModelRequest_ValidateError(req => req with { Name = string.Empty }, "Name");
    }
}
