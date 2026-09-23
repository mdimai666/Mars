using System.Text.Json.Nodes;
using AutoFixture;
using FluentAssertions;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.PostJsons;
using Mars.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;
using Mars.WebApiClient.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient.Integration.Tests.Tests.PostJsons;

/// <summary>
/// Регрессии JSON-пути записи мета-значений: File/Image (Guid в model_id),
/// мульти-значения массивом для не-ссылочных типов, round-trip Excerpt/LangCode.
/// </summary>
public sealed class PostJsonMetaTests : BaseWebApiClientTests
{
    public PostJsonMetaTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task UpdatePostJson_ImageField_WritesModelId()
    {
        _ = nameof(IPostJsonServiceClient.Update);
        var client = GetWebApiClient();
        var entity = await CreateEntity<PostEntity>();
        SetupMetaFields([new MetaFieldEntity { Type = EMetaFieldType.Image, Key = "img", Title = "Image" }]);
        var fileId = Guid.NewGuid();

        var request = _fixture.Create<UpdatePostJsonRequest>() with
        {
            Id = entity.Id,
            Type = "post",
            Meta = new Dictionary<string, JsonNode>
            {
                ["img"] = JsonValue.Create(fileId.ToString())!,
            },
        };

        await client.PostJson.Update(request);

        var ef = AppFixture.MarsDbContext();
        var dbEntity = await ef.Posts.AsNoTracking()
                                    .Include(s => s.MetaValues!)
                                    .ThenInclude(s => s.MetaField)
                                    .FirstAsync(s => s.Id == entity.Id);
        var row = dbEntity.MetaValues.Should().ContainSingle().Subject;
        row.MetaField.Key.Should().Be("img");
        row.ModelId.Should().Be(fileId);
    }

    [IntegrationFact]
    public async Task UpdatePostJson_MultipleStringArray_WritesRowsWithIndex()
    {
        _ = nameof(IPostJsonServiceClient.Update);
        var client = GetWebApiClient();
        var entity = await CreateEntity<PostEntity>();
        SetupMetaFields([new MetaFieldEntity { Type = EMetaFieldType.String, Key = "strs", Title = "Strings", IsMultiple = true }]);

        var request = _fixture.Create<UpdatePostJsonRequest>() with
        {
            Id = entity.Id,
            Type = "post",
            Meta = new Dictionary<string, JsonNode>
            {
                ["strs"] = new JsonArray("первый", "второй"),
            },
        };

        await client.PostJson.Update(request);

        var ef = AppFixture.MarsDbContext();
        var dbEntity = await ef.Posts.AsNoTracking()
                                    .Include(s => s.MetaValues!)
                                    .ThenInclude(s => s.MetaField)
                                    .FirstAsync(s => s.Id == entity.Id);
        var rows = dbEntity.MetaValues!.OrderBy(s => s.Index).ToList();
        rows.Should().HaveCount(2);
        rows[0].Index.Should().Be(0);
        rows[0].Get().Should().Be("первый");
        rows[1].Index.Should().Be(1);
        rows[1].Get().Should().Be("второй");
    }

    [IntegrationFact]
    public async Task PostJson_ExcerptAndLang_RoundTripThroughGetAndUpdate()
    {
        _ = nameof(IPostJsonServiceClient.Get);
        _ = nameof(IPostJsonServiceClient.Update);
        var client = GetWebApiClient();
        var entity = await CreateEntity<PostEntity>();

        var ef = AppFixture.MarsDbContext();
        var dbPost = await ef.Posts.FirstAsync(s => s.Id == entity.Id);
        dbPost.Excerpt = "анонс";
        dbPost.LangCode = "ru";
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        // чтение отдаёт excerpt/lang
        var got = await client.PostJson.Get(entity.Id, renderContent: false);
        got.Should().NotBeNull();
        got!.Excerpt.Should().Be("анонс");
        got.LangCode.Should().Be("ru");

        // read-modify-write из ответа не затирает excerpt/lang
        var request = _fixture.Create<UpdatePostJsonRequest>() with
        {
            Id = got.Id,
            Title = "новый заголовок",
            Type = got.Type,
            Slug = got.Slug,
            Excerpt = got.Excerpt,
            LangCode = got.LangCode,
            Meta = null,
        };

        await client.PostJson.Update(request);

        var dbEntity = await ef.Posts.AsNoTracking().FirstAsync(s => s.Id == entity.Id);
        dbEntity.Title.Should().Be("новый заголовок");
        dbEntity.Excerpt.Should().Be("анонс");
        dbEntity.LangCode.Should().Be("ru");
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
