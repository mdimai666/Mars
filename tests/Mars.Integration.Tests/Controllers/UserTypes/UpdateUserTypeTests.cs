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
using Mars.Shared.Contracts.UserTypes;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.UserTypes;

/// <seealso cref="UserTypeController.Update(Guid, UpdateUserTypeRequest, CancellationToken)"/>
public class UpdateUserTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/UserType";

    public UpdateUserTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdateUserType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(UserTypeController.Update);
        _ = nameof(UserTypeRepository.Update);
        var client = AppFixture.GetClient();

        var postType = _fixture.Create<UserTypeEntity>();
        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToArray();
        postType.MetaFields = [.. metaFields];
        ef.UserTypes.Add(postType);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        var updatingId = postType.Id;

        var metafieldUpdateItem = _fixture.Create<UpdateMetaFieldRequest>() with
        {
            Id = metaFields[0].Id,
        };

        var request = _fixture.Create<UpdateUserTypeRequest>() with
        {
            Id = postType.Id,
            MetaFields = [_fixture.Create<UpdateMetaFieldRequest>(), metafieldUpdateItem]
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError().ReceiveJson<UserTypeSummaryResponse>();

        //Assert
        ef.ChangeTracker.Clear();
        result.Should().NotBeNull();
        result.Title.Should().Be(request.Title);

        var postTypeEntity = ef.UserTypes.Include(s => s.MetaFields!)
                                                .ThenInclude(s => s.Variants)
                                            .FirstOrDefault(s => s.Id == updatingId);
        postTypeEntity.Should().NotBeNull();
        postTypeEntity.Should().BeEquivalentTo(request, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdateUserTypeRequest>()
            .Excluding(s => s.MetaFields)
            .ExcludingMissingMembers());
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
