using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Core.Extensions;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Services;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostCategories;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.PostCategories;

/// <seealso cref="Mars.Controllers.PostCategoryController"/>
public sealed class UpdatePostCategoryTests : ApplicationTests
{
    const string _apiUrl = "/api/PostCategory";

    public UpdatePostCategoryTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdatePostCategory_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryController.Update);
        _ = nameof(PostCategoryRepository.Update);
        var client = AppFixture.GetClient();

        var createdPostCategory = _fixture.Create<PostCategoryEntity>();
        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToList();
        var postCategoryType = ef.PostCategoryTypes.Include(s => s.MetaFields).First(s => s.TypeName == PostCategoryTypeEntity.DefaultTypeName);
        postCategoryType.MetaFields = metaFields;
        var metaValues = metaFields.Select(mf =>
        {
            var mv = _fixture.MetaValueEntity(mf.Id, mf.Type);
            mv.MetaField = mf;
            return mv;
        }).ToList();
        createdPostCategory.MetaValues = metaValues;
        ef.PostCategories.Add(createdPostCategory);
        ef.SaveChanges();
        AppFixture.ServiceProvider.GetRequiredService<IPostCategoryMetaLocator>().InvalidateCompiledMetaMtoModels();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var metaValueUpdates = metaValues.Select((mv, i) => _fixture.UpdateSimpleCreateMetaValueRequest(i != 0 ? mv.Id : Guid.NewGuid(), mv.MetaField.Id, mv.MetaField.Type)).ToArray();

        var postCategory = _fixture.Create<UpdatePostCategoryRequest>() with
        {
            Id = createdPostCategory.Id,
            Type = PostCategoryTypeEntity.DefaultTypeName,
            PostType = "post",
            MetaValues = metaValueUpdates,
            //PathIds = [createdPostCategory.Id],
            ParentId = null,
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(postCategory).CatchUserActionError().ReceiveJson<PostCategoryDetailResponse>();

        //Assert
        result.Should().NotBeNull();

        ef.ChangeTracker.Clear();
        var dbPostCategory = ef.PostCategories.Include(s => s.MetaValues).Include(s => s.PostType).FirstOrDefault(s => s.Id == postCategory.Id);
        dbPostCategory.Should().NotBeNull();

        dbPostCategory.Should().BeEquivalentTo(postCategory, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdatePostCategoryRequest>()
            .Excluding(s => s.MetaValues)
            .Excluding(s => s.PostType)
            .ExcludingMissingMembers());

        dbPostCategory.MetaValues.Should().AllSatisfy(e =>
        {
            var req = postCategory.MetaValues.First(s => s.Id == e.Id);
            e.Should().BeEquivalentTo(req, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<UpdateMetaValueRequest>()
                //.Excluding(s => s.DateTime)
                .ExcludingMissingMembers());
            //e.DateTime.Date.ToString("g").Should().Be(req.DateTime.Date.ToString("g"));

        });

        dbPostCategory.PostType.TypeName.Should().Be(result.PostType);

        var postType = ef.PostTypes.Include(s => s.PostCategories).First(s => s.TypeName == "post");
        postType.PostCategories.Should().ContainSingle(s => s.Id == result.Id);
    }

    [IntegrationFact]
    public async Task UpdatePostCategory_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(PostCategoryController.Update);
        _ = nameof(PostCategoryService.Update);
        var client = AppFixture.GetClient();

        var updatePostCategoryRequest = _fixture.Create<UpdatePostCategoryRequest>();
        updatePostCategoryRequest = updatePostCategoryRequest with
        {
            Type = "invalid_type",
        };

        var expectError = new Dictionary<string, string[]>()
        {
            [nameof(UpdatePostCategoryRequest.Type)] = ["*exist*"],
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(updatePostCategoryRequest).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.Should().HaveSameCount(expectError);
        result.Errors.Should().AllSatisfy(x =>
        {
            foreach (var pattern in expectError[x.Key])
            {
                x.Value.Should().ContainMatch(pattern);
            }
            //expectError[x.Key].Should().ContainMatch(x.Value); //order insensetive
        });
    }

    [IntegrationFact]
    public async Task UpdatePostCategory_UpdateParentSlug_ShouldChildElementRecalcPath()
    {
        //Arrange
        _ = nameof(PostCategoryRepository.Update);
        _ = nameof(PostCategoryController.Update);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var postCategoryType = ef.PostCategoryTypes.Include(s => s.MetaFields).First(s => s.TypeName == PostCategoryTypeEntity.DefaultTypeName);

        var catParent = _fixture.Create<CreatePostCategoryRequest>() with
        {
            Type = postCategoryType.TypeName,
            PostType = "post",
            MetaValues = [],
            Slug = "parent1",
        };
        var catChild = _fixture.Create<CreatePostCategoryRequest>() with
        {
            Type = postCategoryType.TypeName,
            PostType = "post",
            MetaValues = [],
            Slug = "child1",
            ParentId = catParent.Id,
        };
        await client.Request(_apiUrl).PostJsonAsync(catParent);
        await client.Request(_apiUrl).PostJsonAsync(catChild);

        var updatedParent = catParent.CopyViaJsonConversion<UpdatePostCategoryRequest>() with
        {
            Slug = "parentNewPath"
        };

        Guid[] expectPathIds = [updatedParent.Id, catChild.Id!.Value];
        var expectSlugPath = $"/{updatedParent.Id}/{catChild.Id}";
        var expectPath = '/' + string.Join('/', expectPathIds);

        //Act
        var res = await client.Request(_apiUrl).PutJsonAsync(updatedParent).CatchUserActionError();
        var resultChild = await client.Request(_apiUrl, catChild.Id).GetJsonAsync<PostCategoryDetailResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        resultChild.Should().NotBeNull();

        resultChild.PathIds.Should().BeEquivalentTo(expectPathIds);
        resultChild.Path.Should().Be(expectPath);
        resultChild.SlugPath.Should().Be(expectSlugPath);
    }

    [IntegrationFact]
    public async Task UpdatePostCategory_UpdateParentElement_ShouldRecalcChildTree()
    {
        //Arrange
        _ = nameof(PostCategoryRepository.Update);
        _ = nameof(PostCategoryController.Update);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var postCategoryType = ef.PostCategoryTypes.Include(s => s.MetaFields).First(s => s.TypeName == PostCategoryTypeEntity.DefaultTypeName);

        var catParent = _fixture.Create<CreatePostCategoryRequest>() with
        {
            Type = postCategoryType.TypeName,
            PostType = "post",
            MetaValues = [],
            Slug = "parent1",
        };
        var item2InRoot = _fixture.Create<CreatePostCategoryRequest>() with
        {
            Type = postCategoryType.TypeName,
            PostType = "post",
            MetaValues = [],
            Slug = "item1",
        };
        var catChild = _fixture.Create<CreatePostCategoryRequest>() with
        {
            Type = postCategoryType.TypeName,
            PostType = "post",
            MetaValues = [],
            Slug = "child2",
            ParentId = item2InRoot.Id!.Value
        };
        var catChild3 = _fixture.Create<CreatePostCategoryRequest>() with
        {
            Type = postCategoryType.TypeName,
            PostType = "post",
            MetaValues = [],
            Slug = "child3",
            ParentId = catChild.Id!.Value
        };
        await client.Request(_apiUrl).PostJsonAsync(catParent);
        await client.Request(_apiUrl).PostJsonAsync(item2InRoot);
        await client.Request(_apiUrl).PostJsonAsync(catChild);
        await client.Request(_apiUrl).PostJsonAsync(catChild3);

        /* ==idea==
        has:
        -catParent
        -item2InRoot(this)
            -catChild
                -catChild3
        will be:
        -catParent
            -item2InRoot
                -catChild
                    -catChild3

         */

        //Connect To Parent
        var updateItem2 = item2InRoot.CopyViaJsonConversion<UpdatePostCategoryRequest>() with
        {
            Slug = "item1-updated",
            ParentId = catParent.Id!.Value
        };

        Guid[] expectPathIds = [catParent.Id!.Value, updateItem2.Id, catChild.Id!.Value];
        var expectSlugPath = $"/{catParent.Slug}/{updateItem2.Slug}/{catChild.Slug}";
        var expectPath = '/' + string.Join('/', expectPathIds);

        //Act
        var res = await client.Request(_apiUrl).PutJsonAsync(updateItem2).CatchUserActionError();
        var resultChild = await client.Request(_apiUrl, catChild.Id).GetJsonAsync<PostCategoryDetailResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        resultChild.Should().NotBeNull();

        resultChild.PathIds.Should().BeEquivalentTo(expectPathIds);
        resultChild.Path.Should().Be(expectPath);
        resultChild.SlugPath.Should().Be(expectSlugPath);

        //Проверяем внука
        var grandson = await client.Request(_apiUrl, catChild3.Id).GetJsonAsync<PostCategoryDetailResponse>();
        grandson.PathIds.Should().BeEquivalentTo([.. expectPathIds, grandson.Id]);
        grandson.Path.Should().Be(expectPath + '/' + grandson.Id);
        grandson.SlugPath.Should().Be(expectSlugPath + '/' + grandson.Slug);

    }

}
