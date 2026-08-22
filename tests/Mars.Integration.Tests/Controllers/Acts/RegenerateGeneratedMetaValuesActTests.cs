using System.Text.Json.Nodes;
using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostJsons;
using Mars.Shared.Contracts.XActions;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Acts;

/// <summary>Перегенерация значений полей-генераторов у существующих постов (mars.content.regenerateGeneratedMetaValues)</summary>
public class RegenerateGeneratedMetaValuesActTests : ApplicationTests
{
    public RegenerateGeneratedMetaValuesActTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task Inject_RegenerateAll_RenumbersOldPosts_AndNewPostsContinue()
    {
        //Arrange: посты созданы ДО появления поля-генератора — значения пустые
        var client = AppFixture.GetClient();
        for (var i = 0; i < 3; i++)
        {
            var post = _fixture.Create<CreatePostJsonRequest>() with { Type = "post", Meta = null };
            var res = await client.Request("/api/PostJson").AllowAnyHttpStatus().PostJsonAsync(post);
            res.StatusCode.Should().Be(StatusCodes.Status201Created);
        }

        var ef = AppFixture.MarsDbContext();
        var field = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "Number",
            Key = $"num_{Guid.NewGuid():N}"[..12],
            Type = EMetaFieldType.String,
            CreatedAt = DateTimeOffset.Now,
            Options = new JsonObject
            {
                ["generator"] = new JsonObject
                {
                    ["type"] = MetaFieldGeneratorCatalog.Sequence,
                    ["params"] = new JsonObject { ["prefix"] = "ВУ", ["paddingWidth"] = 4 },
                },
            },
        };
        var postType = await ef.PostTypes.Include(s => s.MetaFields).FirstAsync(s => s.TypeName == "post");
        postType.MetaFields = [.. postType.MetaFields, field];
        await ef.MetaFields.AddAsync(field);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        //Act: перегенерация всех постов типа
        var call = new XActionCommandCall
        {
            Id = "mars.content.regenerateGeneratedMetaValues",
            Args = new Dictionary<string, string> { ["postType"] = "post", ["mode"] = "all" },
        };
        var actResult = await client.Request("/api/Act", "Inject")
                                    .PostJsonAsync(call)
                                    .CatchUserActionError()
                                    .ReceiveJson<XActResult>();

        //Assert: старые посты перенумерованы (+ демо-посты сида)
        actResult.Ok.Should().BeTrue();
        ef.ChangeTracker.Clear();
        var postsCount = await ef.Posts.Include(p => p.PostType)
                                       .CountAsync(p => p.PostType.TypeName == "post");
        var values = await ef.PostMetaValues.Where(v => v.MetaFieldId == field.Id)
                                            .Select(v => v.StringShort)
                                            .ToListAsync();
        values.Should().BeEquivalentTo(
            Enumerable.Range(1, postsCount).Select(n => "ВУ" + n.ToString().PadLeft(4, '0')));

        // новые посты продолжают нумерацию после перенумерации
        var newPost = _fixture.Create<CreatePostJsonRequest>() with { Type = "post", Meta = null };
        var created = await client.Request("/api/PostJson").PostJsonAsync(newPost);
        var body = await created.GetJsonAsync<PostJsonResponse>();
        body.Meta[field.Key].ToString().Should().Be("ВУ" + (postsCount + 1).ToString().PadLeft(4, '0'));
    }
}
