using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.PostTypes;

/// <seealso cref="PostTypeController.Update(Guid, UpdatePostTypeRequest, CancellationToken)"/>
public class UpdatePostTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/PostType";

    public UpdatePostTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdatePostType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostTypeController.Update);
        _ = nameof(PostTypeRepository.Update);
        var client = AppFixture.GetClient();

        var postType = _fixture.Create<PostTypeEntity>();
        using var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToArray();
        postType.MetaFields = new(metaFields);
        ef.PostTypes.Add(postType);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        var updatingId = postType.Id;

        var metafieldUpdateItem = _fixture.Create<UpdateMetaFieldRequest>() with
        {
            Id = metaFields[0].Id,
        };

        var request = _fixture.Create<UpdatePostTypeRequest>() with
        {
            Id = postType.Id,
            MetaFields = [_fixture.Create<UpdateMetaFieldRequest>(), metafieldUpdateItem]
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError().ReceiveJson<PostTypeSummaryResponse>();

        //Assert
        ef.ChangeTracker.Clear();
        result.Should().NotBeNull();
        result.Title.Should().Be(request.Title);

        var postTypeEntity = ef.PostTypes.Include(s => s.MetaFields!)
                                                .ThenInclude(s => s.Variants)
                                            .Include(s => s.PostStatusList)
                                            .FirstOrDefault(s => s.Id == updatingId);
        postTypeEntity.Should().NotBeNull();
        postTypeEntity.Should().BeEquivalentTo(request, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdatePostTypeRequest>()
            .WithMapping(nameof(PostTypeEntity.PostContentType), nameof(CreatePostTypeRequest.PostContentSettings))
            .Excluding(s => s.PostStatusList)
            .Excluding(s => s.MetaFields)
            .ExcludingMissingMembers());
        postTypeEntity.PostStatusList.Should().AllSatisfy(e =>
        {
            var req = request.PostStatusList.First(s => s.Id == e.Id);
            e.Should().BeEquivalentTo(req, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<UpdatePostStatusRequest>()
                .ExcludingMissingMembers());
        });
        postTypeEntity.MetaFields.Should().AllSatisfy(e =>
        {
            var req = request.MetaFields.First(s => s.Id == e.Id);
            e.Should().BeEquivalentTo(req, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<UpdateMetaFieldRequest>()
                .Excluding(s => s.Variants)
                .ExcludingMissingMembers());

            e.Variants.Should().AllSatisfy(v =>
            {
                var va = req.Variants!.First(s => s.Id == v.Id);
                v.Should().BeEquivalentTo(va, options => options
                    .ComparingRecordsByValue()
                    .ComparingByMembers<UpdateMetaFieldVariantRequest>()
                    .ExcludingMissingMembers());
            });
        });
    }
}
