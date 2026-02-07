using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Services;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostCategories;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.PostCategories;

public class GetPostCategoryTests : ApplicationTests
{
    const string _apiUrl = "/api/PostCategory";

    public GetPostCategoryTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task GetPostCategory_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryController.Get);
        _ = nameof(PostCategoryService.Get);
        var client = AppFixture.GetClient();

        var createdPostCategory = _fixture.Create<PostCategoryEntity>();

        var ef = AppFixture.MarsDbContext();
        await ef.PostCategories.AddAsync(createdPostCategory);
        await ef.SaveChangesAsync();

        var postCategoryTypeId = createdPostCategory.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(postCategoryTypeId).GetJsonAsync<PostCategoryDetailResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetPostCategoryBySlug_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryController.GetBySlug);
        _ = nameof(PostCategoryService.GetDetailBySlug);
        var client = AppFixture.GetClient();

        var createdPostCategory = _fixture.Create<PostCategoryEntity>();

        var ef = AppFixture.MarsDbContext();
        await ef.PostCategories.AddAsync(createdPostCategory);
        await ef.SaveChangesAsync();

        //Act
        var typeName = PostCategoryTypeEntity.DefaultTypeName;
        var result = await client.Request(_apiUrl, $"by-type/{typeName}/item/{createdPostCategory.Slug}")
                                .GetJsonAsync<PostCategoryDetailResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetPostCategory_InvalidModelRequest_Fail404()
    {
        //Arrange
        _ = nameof(PostCategoryController.Get);
        _ = nameof(PostCategoryService.Get);
        var client = AppFixture.GetClient();
        var invalidPostCategoryId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(invalidPostCategoryId)
                        .AllowAnyHttpStatus().SendAsync(HttpMethod.Get);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task ListPostCategory_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryController.List);
        _ = nameof(PostCategoryService.List);
        var client = AppFixture.GetClient();

        var createdPostCategories = _fixture.CreateMany<PostCategoryEntity>(3);

        var ef = AppFixture.MarsDbContext();
        await ef.PostCategories.AddRangeAsync(createdPostCategories);
        await ef.SaveChangesAsync();

        var expectCount = ef.PostCategories.Count();

        //Act
        var result = await client.Request(_apiUrl, "list/offset").GetJsonAsync<ListDataResult<PostCategoryListItemResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(expectCount);
    }

    [IntegrationFact]
    public async Task ListPostCategory_RequestByPostCategoryTypeName_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryController.List);
        _ = nameof(PostCategoryService.List);
        var client = AppFixture.GetClient();

        var createdPostCategories = _fixture.CreateMany<PostCategoryEntity>(3);

        var ef = AppFixture.MarsDbContext();
        await ef.PostCategories.AddRangeAsync(createdPostCategories);
        await ef.SaveChangesAsync();

        var typeName = PostCategoryTypeEntity.DefaultTypeName;
        var expectCount = ef.PostCategories.Include(s => s.PostCategoryType).Count(s => s.PostCategoryType.TypeName == typeName);

        //Act
        var result = await client.Request(_apiUrl, $"by-type/{typeName}/list/offset").GetJsonAsync<ListDataResult<PostCategoryListItemResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(expectCount);
    }

    [IntegrationFact]
    public async Task ListPostCategory_SearchRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryController.List);
        _ = nameof(PostCategoryService.List);
        var client = AppFixture.GetClient();

        var searchString = $"{Guid.NewGuid()}";
        var searchTitleString = $"nonUsePart_{searchString}";
        var typeName = PostCategoryTypeEntity.DefaultTypeName;

        var createdPostCategories = _fixture.CreateMany<PostCategoryEntity>(3);
        createdPostCategories.ElementAt(0).Title = searchTitleString;

        var ef = AppFixture.MarsDbContext();
        await ef.PostCategories.AddRangeAsync(createdPostCategories);
        await ef.SaveChangesAsync();

        var expectPostCategoryId = createdPostCategories.ElementAt(0).Id;

        var request = new ListPostCategoryQueryRequest() { Search = searchString };

        //Act
        var result = await client.Request(_apiUrl, $"by-type/{typeName}/list/offset")
                                    .AppendQueryParam(request)
                                    .GetJsonAsync<ListDataResult<PostCategoryListItemResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(1);
        result.Items.ElementAt(0).Id.Should().Be(expectPostCategoryId);
    }

    [IntegrationFact(Skip = "not yet")]
    public async Task GetPostCategory__NonFilledMetaField_ShouldReturnBlankMetaValues()
    {
        //Arrange
        _ = nameof(PostCategoryController.Get);
        _ = nameof(PostCategoryService.Get);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToList();
        var typeName = PostCategoryTypeEntity.DefaultTypeName;
        var postType = ef.PostCategoryTypes.Include(s => s.MetaFields).First(s => s.TypeName == typeName);
        postType.MetaFields = metaFields;
        await ef.MetaFields.AddRangeAsync(metaFields);

        var createdPostCategory = _fixture.Create<PostCategoryEntity>();

        await ef.PostCategories.AddAsync(createdPostCategory);
        await ef.SaveChangesAsync();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var postTypeId = createdPostCategory.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(postTypeId).GetJsonAsync<PostCategoryDetailResponse>();

        //Assert
        //result.me.Should().NotBeNull();
        throw new NotImplementedException();
    }

    [IntegrationFact]
    public async Task GetEditModel_NonFilledMetaField_ShouldReturnBlankMetaValues()
    {
        //Arrange
        _ = nameof(PostCategoryController.GetEditModel);
        _ = nameof(PostCategoryService.GetEditModel);
        _ = nameof(PostCategoryRepository.GetPostCategoryEditDetail);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToList();
        var typeName = PostCategoryTypeEntity.DefaultTypeName;
        var postType = ef.PostCategoryTypes.Include(s => s.MetaFields).First(s => s.TypeName == typeName);
        postType.MetaFields = metaFields;
        await ef.MetaFields.AddRangeAsync(metaFields);

        var createdPostCategory = _fixture.Create<PostCategoryEntity>();

        await ef.PostCategories.AddAsync(createdPostCategory);
        await ef.SaveChangesAsync();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        //Act
        var result = await client.Request(_apiUrl, "edit", createdPostCategory.Id).GetJsonAsync<PostCategoryEditViewModel>();

        //Assert
        result.Should().NotBeNull();
        result.PostCategory.MetaValues.Should().HaveCount(metaFields.Count);
    }

    [IntegrationFact]
    public async Task ListPostCategory_ReturnAsSortedByPath_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryController.List);
        _ = nameof(PostCategoryService.List);
        var client = AppFixture.GetClient();

        var createdPostCategories = _fixture.CreateMany<PostCategoryEntity>(6).ToArray();

        var x = createdPostCategories;

        void set(int index, string slug, int parentIndex = -1)
        {
            createdPostCategories[index].Slug = slug;
            if (parentIndex > -1)
                createdPostCategories[index].ParentId = createdPostCategories[parentIndex].Id;
        }

        set(0, "2_tree");
        set(1, "child", 0);
        set(2, "grandson", 1);
        set(3, "3_last_item");
        set(4, "1_tree");
        set(5, "3_last_item_child", 3);

        var ef = AppFixture.MarsDbContext();
        await ef.PostCategories.AddRangeAsync(createdPostCategories);
        await ef.SaveChangesAsync();
        await AppFixture.ServiceProvider.GetRequiredService<IPostCategoryRepository>().RecalculateAllCategoryPathsHierarchy(default);

        string[] expectOrderedPathList = [
            "/1_tree",
            "/2_tree",
            "/2_tree/child",
            "/2_tree/child/grandson",
            "/3_last_item",
            "/3_last_item/3_last_item_child",
            ];

        //Act
        var result = await client.Request(_apiUrl, "list/offset").GetJsonAsync<ListDataResult<PostCategoryListItemResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Select(s => s.SlugPath).Should().BeEquivalentTo(expectOrderedPathList, options => options.WithStrictOrdering());
    }
}
