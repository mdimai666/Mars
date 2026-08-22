using System.Text.Json.Nodes;
using FluentAssertions;
using Flurl.Http;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostJsons;
using Mars.Shared.Contracts.Posts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Posts;

/// <summary>Фильтры колонок грида постов (базовые колонки + мета-поля)</summary>
public class ListPostGridFilterTests : ApplicationTests
{
    const string _listUrl = "/api/Post/by-type/post/list/page";

    readonly string _run = Guid.NewGuid().ToString("N")[..8];

    public ListPostGridFilterTests(ApplicationFixture appFixture) : base(appFixture) { }

    async Task<(string CodeKey, string NumKey)> AddFieldsAsync()
    {
        var ef = AppFixture.MarsDbContext();
        var codeField = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "Code",
            Key = $"code_{_run}",
            Type = EMetaFieldType.String,
            IsNullable = true,
            CreatedAt = DateTimeOffset.Now,
        };
        var numField = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "Num",
            Key = $"num_{_run}",
            Type = EMetaFieldType.Int,
            IsNullable = true,
            CreatedAt = DateTimeOffset.Now,
        };

        var postType = await ef.PostTypes.Include(s => s.MetaFields).FirstAsync(s => s.TypeName == "post");
        postType.MetaFields = [.. postType.MetaFields, codeField, numField];
        await ef.MetaFields.AddRangeAsync(codeField, numField);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        return (codeField.Key, numField.Key);
    }

    static async Task CreatePostAsync(IFlurlClient client, string title, string? status, IReadOnlyDictionary<string, JsonNode>? meta)
    {
        var request = new CreatePostJsonRequest { Title = title, Type = "post", Status = status, Meta = meta };

        var response = await client.Request("/api/PostJson").AllowAnyHttpStatus().PostJsonAsync(request);
        if (response.StatusCode != StatusCodes.Status201Created)
            throw new InvalidOperationException($"create failed {(int)response.StatusCode}: {await response.GetStringAsync()}");
    }

    static Task<PagingResult<PostListItemResponse>> ListAsync(IFlurlClient client, TablePostQueryRequest request)
        => client.Request(_listUrl).PostJsonAsync(request).ReceiveJson<PagingResult<PostListItemResponse>>();

    /// <summary>GET list/offset с фильтрами в query (indexed-формат, как сериализует веб-клиент) — путь грида</summary>
    static Task<ListDataResult<PostListItemResponse>> ListOffsetAsync(IFlurlClient client, params PostGridFilter[] filters)
    {
        var request = client.Request("/api/Post/by-type/post/list/offset").AppendQueryParam(new { Skip = 0, Take = 50 });

        var i = 0;
        foreach (var filter in filters)
        {
            request.AppendQueryParam($"Filters[{i}].Key", filter.Key)
                   .AppendQueryParam($"Filters[{i}].Op", filter.Op);

            if (filter.Value is not null)
                request.AppendQueryParam($"Filters[{i}].Value", filter.Value);

            if (filter.Values is not null)
            {
                for (var j = 0; j < filter.Values.Length; j++)
                    request.AppendQueryParam($"Filters[{i}].Values[{j}]", filter.Values[j]);
            }

            i++;
        }

        return request.GetJsonAsync<ListDataResult<PostListItemResponse>>();
    }

    [IntegrationFact]
    public async Task ListTable_Filters_Title_MetaString_NumberRange_Presence()
    {
        //Arrange
        var client = AppFixture.GetClient();
        var (codeKey, numKey) = await AddFieldsAsync();

        await CreatePostAsync(client, $"Alpha {_run}", "draft", new Dictionary<string, JsonNode>
        {
            [codeKey] = $"ABC-{_run}",
            [numKey] = 5,
        });
        await CreatePostAsync(client, $"Beta {_run}", "draft", new Dictionary<string, JsonNode>
        {
            [codeKey] = $"abc-{_run} second",
            [numKey] = 10,
        });
        await CreatePostAsync(client, $"Gamma {_run}", "publish", new Dictionary<string, JsonNode>
        {
            [codeKey] = $"XYZ-{_run}",
            [numKey] = 15,
        });

        //Act/Assert: заголовок — поиск подстроки
        var byTitle = await ListAsync(client, new TablePostQueryRequest
        {
            PageSize = 50,
            Filters = [new PostGridFilter { Key = "title", Op = PostGridFilterOps.Contains, Value = $"Alpha {_run}" }],
        });
        byTitle.TotalCount.Should().Be(1);

        // тот же фильтр через GET (query) — путь грида админки
        var byTitleOffset = await ListOffsetAsync(client,
            new PostGridFilter { Key = "title", Op = PostGridFilterOps.Contains, Value = $"Alpha {_run}" });
        byTitleOffset.TotalCount.Should().Be(1);

        // мета-строка — без учёта регистра (оба значения «ABC-…» и «abc-…»)
        var byMetaString = await ListAsync(client, new TablePostQueryRequest
        {
            PageSize = 50,
            Filters = [new PostGridFilter { Key = codeKey, Op = PostGridFilterOps.Contains, Value = $"abc-{_run}" }],
        });
        byMetaString.TotalCount.Should().Be(2);

        // мета-число — диапазон от
        var byNumberFrom = await ListAsync(client, new TablePostQueryRequest
        {
            PageSize = 50,
            Filters = [new PostGridFilter { Key = numKey, Op = PostGridFilterOps.Gte, Value = "10" }],
        });
        byNumberFrom.TotalCount.Should().Be(2);

        // мета-число — диапазон от…до
        var byNumberRange = await ListAsync(client, new TablePostQueryRequest
        {
            PageSize = 50,
            Filters =
            [
                new PostGridFilter { Key = numKey, Op = PostGridFilterOps.Gte, Value = "6" },
                new PostGridFilter { Key = numKey, Op = PostGridFilterOps.Lte, Value = "12" },
            ],
        });
        byNumberRange.TotalCount.Should().Be(1);

        // заполнено / пусто
        var notEmpty = await ListAsync(client, new TablePostQueryRequest
        {
            PageSize = 50,
            Filters = [new PostGridFilter { Key = codeKey, Op = PostGridFilterOps.NotEmpty }],
        });
        notEmpty.TotalCount.Should().Be(3);

        var empty = await ListAsync(client, new TablePostQueryRequest
        {
            PageSize = 50,
            Filters = [new PostGridFilter { Key = codeKey, Op = PostGridFilterOps.Empty }],
        });
        empty.TotalCount.Should().BeGreaterThan(0); // демо-посты сида без значения поля
    }

    [IntegrationFact]
    public async Task ListTable_StatusIn_FiltersByStatuses()
    {
        //Arrange
        var client = AppFixture.GetClient();
        _ = await AddFieldsAsync();

        var before = await ListAsync(client, new TablePostQueryRequest
        {
            PageSize = 50,
            Filters = [new PostGridFilter { Key = "status", Op = PostGridFilterOps.In, Values = ["draft"] }],
        });

        await CreatePostAsync(client, $"DraftOne {_run}", "draft", null);
        await CreatePostAsync(client, $"DraftTwo {_run}", "draft", null);
        await CreatePostAsync(client, $"Published {_run}", "publish", null);

        //Act
        var after = await ListAsync(client, new TablePostQueryRequest
        {
            PageSize = 50,
            Filters = [new PostGridFilter { Key = "status", Op = PostGridFilterOps.In, Values = ["draft"] }],
        });

        //Assert
        after.TotalCount.Should().Be(before.TotalCount + 2);
    }
}
