using System.Text.Json.Nodes;
using AutoFixture;
using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.PostJsons;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;
using Mars.WebApiClient.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient.Integration.Tests.Tests.PostJsons;

public sealed class UpdatePostJsonTests : BaseWebApiClientTests
{
    GeneralUpdateTests<PostEntity, UpdatePostJsonRequest, PostJsonResponse> _updateTest;

    public UpdatePostJsonTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _updateTest = new(this, (client, req) => client.PostJson.Update(req));

    }

    [IntegrationFact]
    public async Task UpdatePostJson_Request_Unauthorized()
    {
        await _updateTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task UpdatePostJson_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(IPostJsonServiceClient.Update);
        var client = GetWebApiClient();
        var entity = await CreateEntity<PostEntity>();
        var mf = new MetaFieldEntity { Type = EMetaFieldType.Int, Key = "int1", Title = "Int 1" };
        SetupMetaFields([mf]);

        var request = _fixture.Create<UpdatePostJsonRequest>() with
        {
            Id = entity.Id,
            Title = "new Title",
            Type = "post",
            Meta = new Dictionary<string, JsonValue>
            {
                ["int1"] = JsonValue.Create(42)!
            }
        };

        //Act
        var result = await client.PostJson.Update(request);

        //Assert
        var ef = AppFixture.MarsDbContext();
        var dbEntity = await ef.Posts.AsNoTracking()
                                    .Include(s => s.MetaValues!)
                                    .ThenInclude(s => s.MetaField)
                                    .FirstAsync(s => s.Id == entity.Id);
        dbEntity.Should().BeEquivalentTo(request, options => options
                    .ComparingRecordsByValue()
                    .ComparingByMembers<UpdatePostJsonRequest>()
                    .Excluding(s => s.Meta)
                    .ExcludingMissingMembers());
        dbEntity.MetaValues.Count.Should().Be(1);
        dbEntity.MetaValues.First().Get().Should().BeEquivalentTo(42);
    }

    [IntegrationFact]
    public void UpdatePostJson_InvalidModelRequest_ValidateError()
    {
        _updateTest.InvalidModelRequest_ValidateError(req => req with { Title = string.Empty }, "Title");
    }

    private void SetupMetaFields(List<MetaFieldEntity> metaFields)
    {
        var ef = AppFixture.MarsDbContext();
        var postType = ef.PostTypes.Include(s => s.MetaFields).First(s => s.TypeName == "post");
        postType.MetaFields = metaFields;
        ef.MetaFields.AddRange(metaFields);
        ef.SaveChanges();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();
    }
}
