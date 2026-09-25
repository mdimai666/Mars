using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Cms.Host.Controllers;
using Mars.Data.Entities;
using Mars.Data.Repositories;
using Mars.Forms.Contracts;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Nodes;

namespace Mars.Integration.Tests.Controllers.PostTypes;

/// <seealso cref="PostTypeController.Update(Guid, UpdatePostTypeRequest, CancellationToken)"/>
public class UpdatePostTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/PostType";

    public UpdatePostTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task UpdatePostType_ValidRequest_Succeeds()
    {
        //Arrange
        _ = nameof(PostTypeController.Update);
        _ = nameof(PostTypeRepository.Update);
        var client = AppFixture.GetClient();

        var postType = _fixture.Create<PostTypeEntity>();
        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToArray();
        postType.MetaFields = metaFields.ToList();
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
                                            .Include(s => s.Statuses)
                                            .FirstOrDefault(s => s.Id == updatingId);
        postTypeEntity.Should().NotBeNull();
        postTypeEntity.Should().BeEquivalentTo(request, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdatePostTypeRequest>()
            .Excluding(s => s.PostStatusList)
            .Excluding(s => s.MetaFields)
            .ExcludingMissingMembers());
        postTypeEntity.Statuses.Should().AllSatisfy(e =>
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

    [IntegrationFact]
    public async Task UpdatePostType_WithSystemFields_StoresThemInTypeOptions()
    {
        //Arrange
        _ = nameof(PostTypeController.Update);
        _ = nameof(PostTypeController.GetEditModel);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var postType = _fixture.Create<PostTypeEntity>();
        postType.TypeName = $"sys{Guid.NewGuid():N}"[..12];
        postType.EnabledFeatures = [PostTypeConstants.Features.Excerpt];
        postType.Options = null;

        ef.PostTypes.Add(postType);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        var request = _fixture.Create<UpdatePostTypeRequest>() with
        {
            Id = postType.Id,
            TypeName = postType.TypeName,
            EnabledFeatures = [PostTypeConstants.Features.Excerpt],
            ImageFieldKey = null,
            MetaFields = [],
            PostStatusList = [],
            SystemFields =
            [
                new FormFieldSettings
                {
                    Key = SystemFieldsCatalog.Slug,
                    Rules = [new FormRuleDefinition { Type = FormRuleCatalog.Unique }],
                },
                new FormFieldSettings { Key = SystemFieldsCatalog.Excerpt, Editor = FormEditorCatalog.Multiline },
            ],
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        ef.ChangeTracker.Clear();
        var entity = ef.PostTypes.First(s => s.Id == postType.Id);
        entity.Options.GetSystemFields()!.Select(s => s.Key).Should()
              .Equal(SystemFieldsCatalog.Slug, SystemFieldsCatalog.Excerpt);
        entity.Options.GetFormLayout().Should().BeNull("параметры полей не пишутся в раскладку формы");

        var viewModel = await client.Request(_apiUrl, "edit", postType.Id)
                                    .GetJsonAsync<PostTypeEditViewModel>();

        viewModel.PostType.SystemFields!.Single(s => s.Key == SystemFieldsCatalog.Slug)
                 .Rules.Single().Type.Should().Be(FormRuleCatalog.Unique);
        viewModel.PostType.SystemFields!.Single(s => s.Key == SystemFieldsCatalog.Excerpt)
                 .Editor.Should().Be(FormEditorCatalog.Multiline);
    }

    [IntegrationFact]
    public async Task UpdatePostType_WithoutSystemFields_KeepsStoredOnes()
    {
        //Arrange
        _ = nameof(PostTypeController.Update);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var postType = _fixture.Create<PostTypeEntity>();
        postType.TypeName = $"keep{Guid.NewGuid():N}"[..12];
        postType.Options = ((JsonNode?)null).WithSystemFields(
        [
            new FormFieldSettings { Key = SystemFieldsCatalog.Slug, Rules = [new FormRuleDefinition { Type = FormRuleCatalog.Unique }] },
        ]);

        ef.PostTypes.Add(postType);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        var request = _fixture.Create<UpdatePostTypeRequest>() with
        {
            Id = postType.Id,
            TypeName = postType.TypeName,
            MetaFields = [],
            PostStatusList = [],
            SystemFields = null,
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        ef.ChangeTracker.Clear();
        ef.PostTypes.First(s => s.Id == postType.Id).Options.GetSystemFields()!
          .Single().Key.Should().Be(SystemFieldsCatalog.Slug, "null в запросе сохранённые параметры не трогает");
    }
}
