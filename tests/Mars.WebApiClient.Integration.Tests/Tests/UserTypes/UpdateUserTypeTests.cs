using AutoFixture;
using FluentAssertions;
using Mars.Core.Exceptions;
using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.UserTypes;
using Mars.Test.Common.FixtureCustomizes;

namespace Mars.WebApiClient.Integration.Tests.Tests.UserTypes;

public sealed class UpdateUserTypeTests : BaseWebApiClientTests
{
    public UpdateUserTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdateUserType_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);
        var request = _fixture.Create<UpdateUserTypeRequest>();

        //Act
        var action = () => client.UserType.Update(request);

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task UpdateUserType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var entity = await CreateEntity<UserTypeEntity>();
        var request = _fixture.Create<UpdateUserTypeRequest>() with
        {
            Id = entity.Id,
            TypeName = entity.TypeName,
        };

        //Act
        var result = await client.UserType.Update(request);

        //Assert
        var dbEntity = await GetEntity<UserTypeEntity>(entity.Id);
        dbEntity.Should().BeEquivalentTo(request, options => options
                    .ComparingRecordsByValue()
                    .ComparingByMembers<UpdateUserTypeRequest>()
                    .Excluding(s => s.MetaFields) // work in integration test
                    .ExcludingMissingMembers());
    }

    [IntegrationFact]
    public void UpdateUserType_InvalidModelRequest_ValidateError()
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<UpdateUserTypeRequest>();
        request = request with
        {
            Title = string.Empty
        };

        //Act
        var action = () => client.UserType.Update(request);

        //Assert
        action.Should().ThrowAsync<MarsValidationException>().RunSync()
            .And.Errors.Should().ContainKey("Title");
    }
}
