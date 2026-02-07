using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Shared.Dto.PostCategories;
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
public sealed class CreatePostCategoryTests : ApplicationTests
{
    const string _apiUrl = "/api/PostCategory";

    public CreatePostCategoryTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreatePostCategory_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PostCategoryController.Create);
        var client = AppFixture.GetClient(true);

        var postRequest = _fixture.Create<CreatePostCategoryRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(postRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task CreatePostCategory_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryRepository.Create);
        _ = nameof(PostCategoryController.Create);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToList();
        //var postType = ef.PostTypes.First(s => s.TypeName == "post");
        var postCategoryType = ef.PostCategoryTypes.Include(s => s.MetaFields).First(s => s.TypeName == PostCategoryTypeEntity.DefaultTypeName);
        postCategoryType.MetaFields = metaFields;
        ef.MetaFields.AddRange(metaFields);
        ef.SaveChanges();
        AppFixture.ServiceProvider.GetRequiredService<IPostCategoryMetaLocator>().InvalidateCompiledMetaMtoModels();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var metaValueCreateList = metaFields.Select(mf => _fixture.CreateSimpleCreateMetaValueRequest(mf.Id, mf.Type)).ToArray();

        var postCategory = _fixture.Create<CreatePostCategoryRequest>() with
        {
            //PostCategoryTypeId = postCategoryType.Id,
            Type = postCategoryType.TypeName,
            PostType = "post",
            MetaValues = metaValueCreateList
        };

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(postCategory).CatchUserActionError();
        var result = await res.GetJsonAsync<PostCategoryDetailResponse>();

        //Assert
        ef.ChangeTracker.Clear();
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Should().NotBeNull();
        var dbPostCategory = ef.PostCategories.Include(s => s.MetaValues!)
                                .ThenInclude(s => s.MetaField)
                                .Include(s => s.PostType)
                                .FirstOrDefault(s => s.Id == postCategory.Id);
        dbPostCategory.Should().NotBeNull();

        dbPostCategory.Should().BeEquivalentTo(postCategory, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<CreatePostCategoryRequest>()
            .Excluding(s => s.MetaValues)
            .Excluding(s => s.PostType)
            .ExcludingMissingMembers());
        dbPostCategory.MetaValues.Should().AllSatisfy(e =>
        {
            var req = postCategory.MetaValues.First(s => s.Id == e.Id);
            e.Should().BeEquivalentTo(req, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<CreateMetaValueRequest>()
                //.Excluding(s => s.DateTime)
                .ExcludingMissingMembers());
            //e.DateTime.Date.ToString("g").Should().Be(req.DateTime.Date.ToString("g"));

        });
        dbPostCategory.PostType.TypeName.Should().Be(result.PostType);

        var postType = ef.PostTypes.Include(s => s.PostCategories).First(s => s.TypeName == "post");
        postType.PostCategories.Should().ContainSingle(s => s.Id == result.Id);
    }

    [IntegrationFact]
    public async Task CreatePostCategory_ValidateQueryValidator_ShouldFail()
    {
        //Arrange
        _ = nameof(PostCategoryRepository.Create);
        _ = nameof(PostCategoryController.Create);
        _ = nameof(CreatePostCategoryQueryValidator);
        var client = AppFixture.GetClient();

        var post = _fixture.Create<CreatePostCategoryRequest>() with { Type = "invalid_type_name" };

        //Act
        var validate = await client.Request(_apiUrl).PostJsonAsync(post).ReceiveValidationError();

        //Assert
        validate.Errors.Should().HaveCount(1);
        validate.Errors.ElementAt(0).Key.Should().Be(nameof(CreatePostCategoryRequest.Type));
    }

    [IntegrationFact]
    public async Task CreatePostCategory_CreateChildElement_ShouldValidPath()
    {
        //Arrange
        _ = nameof(PostCategoryRepository.Create);
        _ = nameof(PostCategoryController.Create);
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
        await client.Request(_apiUrl).PostJsonAsync(catParent);

        var catChild = _fixture.Create<CreatePostCategoryRequest>() with
        {
            Type = postCategoryType.TypeName,
            PostType = "post",
            MetaValues = [],
            Slug = "child1",
            ParentId = catParent.Id,
        };
        Guid[] expectPathIds = [catParent.Id!.Value, catChild.Id!.Value];
        var expectSlugPath = $"/{catParent.Id}/{catChild.Id}";
        var expectPath = '/' + string.Join('/', expectPathIds);

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(catChild).CatchUserActionError();
        var result = await res.GetJsonAsync<PostCategoryDetailResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Should().NotBeNull();

        result.PathIds.Should().BeEquivalentTo(expectPathIds);
        result.Path.Should().Be(expectPath);
        result.SlugPath.Should().Be(expectSlugPath);
    }
}
