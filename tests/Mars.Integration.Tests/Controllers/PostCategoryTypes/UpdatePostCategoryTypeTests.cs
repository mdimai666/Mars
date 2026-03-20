using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Core.Extensions;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.PostCategoryTypes;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostCategoryTypes;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.PostCategoryTypes;

/// <seealso cref="PostCategoryTypeController.Update(Guid, UpdatePostCategoryTypeRequest, CancellationToken)"/>
public class UpdatePostCategoryTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/PostCategoryType";

    public UpdatePostCategoryTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdatePostCategoryType_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.Update);
        _ = nameof(PostCategoryTypeService.Update);
        var client = AppFixture.GetClient(true);

        var postCategoryTypeRequest = _fixture.Create<UpdatePostCategoryTypeRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PutJsonAsync(postCategoryTypeRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task UpdatePostCategoryType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.Update);
        _ = nameof(PostCategoryTypeRepository.Update);
        var client = AppFixture.GetClient();

        var postCategoryType = _fixture.Create<PostCategoryTypeEntity>();
        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToArray();
        postCategoryType.MetaFields = [.. metaFields];
        ef.PostCategoryTypes.Add(postCategoryType);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        var updatingId = postCategoryType.Id;

        var metafieldUpdateItem = _fixture.Create<UpdateMetaFieldRequest>() with
        {
            Id = metaFields[0].Id,
        };

        var request = _fixture.Create<UpdatePostCategoryTypeRequest>() with
        {
            Id = postCategoryType.Id,
            MetaFields = [_fixture.Create<UpdateMetaFieldRequest>(), metafieldUpdateItem]
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError().ReceiveJson<PostCategoryTypeSummaryResponse>();

        //Assert
        ef.ChangeTracker.Clear();
        result.Should().NotBeNull();
        result.Title.Should().Be(request.Title);

        var postCategoryTypeEntity = ef.PostCategoryTypes.Include(s => s.MetaFields!)
                                                .ThenInclude(s => s.Variants)
                                            .FirstOrDefault(s => s.Id == updatingId);
        postCategoryTypeEntity.Should().NotBeNull();
        postCategoryTypeEntity.Should().BeEquivalentTo(request, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdatePostCategoryTypeRequest>()
            .Excluding(s => s.MetaFields)
            .ExcludingMissingMembers());
        postCategoryTypeEntity.MetaFields.Should().AllSatisfy(e =>
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

    [IntegrationFact]
    public async Task UpdatePostCategoryType_WithDuplicateName_ShouldReturnValidationError()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.Update);
        _ = nameof(PostCategoryTypeService.Update);
        _ = nameof(UpdatePostCategoryTypeQueryValidator);
        var client = AppFixture.GetClient();
        AppFixture.ServiceProvider.GetRequiredService<IPostCategoryMetaLocator>().ExistType(PostCategoryTypeEntity.DefaultTypeName).Should().BeTrue();

        var postCategoryTypeRequest = _fixture.Create<CreatePostCategoryTypeRequest>();
        var created = await client.Request(_apiUrl).PostJsonAsync(postCategoryTypeRequest).ReceiveJson<PostCategoryTypeSummaryResponse>();
        var updateRequest = postCategoryTypeRequest.CopyViaJsonConversion<UpdatePostCategoryTypeRequest>() with
        {
            Id = created.Id,
            TypeName = PostCategoryTypeEntity.DefaultTypeName
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(updateRequest).ReceiveValidationError();

        //Assert
        result.Errors.ValidateSatisfy(new()
        {
            [nameof(CreatePostCategoryTypeRequest.TypeName)] = ["*already exist"],
        });
    }
}
