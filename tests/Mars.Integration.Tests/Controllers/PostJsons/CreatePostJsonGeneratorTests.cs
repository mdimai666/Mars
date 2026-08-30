using System.Text.Json.Nodes;
using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostCategories;
using Mars.Cms.Contracts.PostJsons;
using Mars.Cms.Contracts.PostTypes;
using Mars.Cms.Host.Controllers;
using Mars.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.PostJsons;

/// <summary>Генераторы значений мета-полей (Options.generator) при создании поста</summary>
public class CreatePostJsonGeneratorTests : ApplicationTests
{
    const string _apiUrl = "/api/PostJson";

    public CreatePostJsonGeneratorTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    async Task<MetaFieldEntity> AddGeneratorFieldAsync(JsonObject generatorOptions)
    {
        var ef = AppFixture.MarsDbContext();
        var field = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "Number",
            Key = $"num_{Guid.NewGuid():N}"[..12],
            Type = EMetaFieldType.String,
            IsNullable = false, // обязательное — но с генератором, отсутствие значения в запросе не ошибка
            CreatedAt = DateTimeOffset.Now,
            Options = new JsonObject { ["generator"] = generatorOptions },
        };

        var postType = await ef.PostTypes.Include(s => s.MetaFields).FirstAsync(s => s.TypeName == "post");
        postType.MetaFields = [.. postType.MetaFields, field];
        await ef.MetaFields.AddAsync(field);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        return field;
    }

    static async Task<PostJsonResponse> CreatePostJsonAsync(IFlurlClient client, IReadOnlyCollection<Guid>? categoryIds = null)
    {
        var fixture = new Fixture().Customize(new FixtureCustomize());
        var request = fixture.Create<CreatePostJsonRequest>() with
        {
            Type = "post",
            Meta = null,
            CategoryIds = categoryIds,
        };

        var response = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(request);
        if (response.StatusCode != StatusCodes.Status201Created)
            throw new InvalidOperationException($"create failed {(int)response.StatusCode}: {await response.GetStringAsync()}");
        return await response.GetJsonAsync<PostJsonResponse>();
    }

    [IntegrationFact]
    public async Task CreatePostJson_SequenceGenerator_FillsNumbersSequentially()
    {
        //Arrange
        _ = nameof(PostJsonController.Create);
        _ = nameof(MetaValuesGeneratorService);
        var client = AppFixture.GetClient();
        var field = await AddGeneratorFieldAsync(new JsonObject
        {
            ["type"] = MetaFieldGeneratorCatalog.Sequence,
            ["params"] = new JsonObject { ["prefix"] = "ВУ", ["paddingWidth"] = 4 },
        });

        //Act: обязательное поле без значения в запросе — генератор заполняет сам
        var first = await CreatePostJsonAsync(client);
        var second = await CreatePostJsonAsync(client);

        //Assert
        first.Meta[field.Key].ToString().Should().Be("ВУ0001");
        second.Meta[field.Key].ToString().Should().Be("ВУ0002");
    }

    [IntegrationFact]
    public async Task CreatePostJson_SequenceGenerator_PrefixFromCategory()
    {
        //Arrange
        _ = nameof(PostJsonController.Create);
        var client = AppFixture.GetClient();

        // тип «post» в сиде без фичи категорий — включаем, чтобы пост можно было привязать к категории
        var ef = AppFixture.MarsDbContext();
        var postType = await ef.PostTypes.FirstAsync(s => s.TypeName == "post");
        if (!postType.EnabledFeatures.Contains(PostTypeConstants.Features.Category))
            postType.EnabledFeatures.Add(PostTypeConstants.Features.Category);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var categoryRequest = _fixture.Create<CreatePostCategoryRequest>() with
        {
            Type = PostCategoryTypeEntity.DefaultTypeName,
            PostType = "post",
            MetaValues = [],
        };
        var categoryResponse = await client.Request("/api/PostCategory").PostJsonAsync(categoryRequest);
        categoryResponse.StatusCode.Should().Be(StatusCodes.Status201Created);
        var category = await categoryResponse.GetJsonAsync<PostCategoryDetailResponse>();

        var field = await AddGeneratorFieldAsync(new JsonObject
        {
            ["type"] = MetaFieldGeneratorCatalog.Sequence,
            ["params"] = new JsonObject
            {
                ["prefix"] = "ОБЩ",
                ["paddingWidth"] = 3,
                ["categoryPrefixes"] = new JsonObject { [categoryRequest.Slug] = "ПРЕ" },
            },
        });

        //Act: категория из словаря — её префикс; без категории — дефолтный
        var withCategory = await CreatePostJsonAsync(client, [category.Id]);
        var withoutCategory = await CreatePostJsonAsync(client);

        //Assert
        withCategory.Meta[field.Key].ToString().Should().Be("ПРЕ001");
        withoutCategory.Meta[field.Key].ToString().Should().Be("ОБЩ001");
    }
}
