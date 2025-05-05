using AutoFixture;
using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Posts;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Posts;

public sealed class UpdatePostTests : BaseWebApiClientTests
{
    GeneralUpdateTests<PostEntity, UpdatePostRequest, PostDetailResponse> _updateTest;

    public UpdatePostTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _updateTest = new(this, (client, req) => client.Post.Update(req));

    }

    [IntegrationFact]
    public async Task UpdatePost_Request_Unauthorized()
    {
        await _updateTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task UpdatePost_ValidRequest_ShouldSuccess()
    {
        //await _updateTest.ValidRequest_ShouldSuccess(req => req with { Title = "new Title", Type = "post" });

        //Arrange
        var client = GetWebApiClient();
        var entity = await CreateEntity<PostEntity>();
        var request = _fixture.Create<UpdatePostRequest>() with
        {
            Id = entity.Id,
            Title = "new Title",
            Type = "post",
        };

        //Act
        var result = await client.Post.Update(request);

        //Assert
        var dbEntity = await GetEntity<PostEntity>(entity.Id);
        dbEntity.Should().BeEquivalentTo(request, options => options
                    .ComparingRecordsByValue()
                    .ComparingByMembers<UpdatePostRequest>()
                    .Excluding(s => s.MetaValues) // work in integration test
                    .ExcludingMissingMembers());
    }

    [IntegrationFact]
    public void UpdatePost_InvalidModelRequest_ValidateError()
    {
        _updateTest.InvalidModelRequest_ValidateError(req => req with { Title = string.Empty }, "Title");
    }
}
