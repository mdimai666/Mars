using AutoFixture;
using Mars.Core.Exceptions;
using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.PostTypes;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.PostTypes;

public sealed class UpdatePostTypeTests : BaseWebApiClientTests
{
    public UpdatePostTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdatePostType_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);
        var request = _fixture.Create<UpdatePostTypeRequest>();

        //Act
        var action = () => client.PostType.Update(request);

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task UpdatePostType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var entity = await CreateEntity<PostTypeEntity>();
        var request = _fixture.Create<UpdatePostTypeRequest>() with
        {
            Id = entity.Id,
            TypeName = entity.TypeName,
        };

        //Act
        var result = await client.PostType.Update(request);

        //Assert
        var dbEntity = await GetEntity<PostTypeEntity>(entity.Id);
        dbEntity.Should().BeEquivalentTo(request, options => options
                    .ComparingRecordsByValue()
                    .ComparingByMembers<UpdatePostTypeRequest>()
                    .Excluding(s => s.PostStatusList).Excluding(s => s.MetaFields) // work in integration test
                    .ExcludingMissingMembers());
    }

    [IntegrationFact]
    public void UpdatePostType_InvalidModelRequest_ValidateError()
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<UpdatePostTypeRequest>();
        request = request with
        {
            Title = string.Empty
        };

        //Act
        var action = () => client.PostType.Update(request);

        //Assert
        action.Should().ThrowAsync<MarsValidationException>().RunSync()
            .And.Errors.Should().ContainKey("Title");
    }
}
