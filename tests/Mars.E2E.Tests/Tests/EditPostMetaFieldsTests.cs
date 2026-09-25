using System.Text.Json;
using FluentAssertions;
using Flurl.Http;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Cms.Contracts.Posts;
using Mars.E2E.Tests.Fixtures;
using Mars.E2E.Tests.Helpers;
using Mars.Integration.Tests.Attributes;
using Microsoft.Playwright;

namespace Mars.E2E.Tests.Tests;

/// <summary>
/// Метаполя в форме поста: тип поста дополняется метаполями через API, значения правят редакторы
/// общего слоя (строка и текст), а хранятся они в EAV-строках владельца.
/// </summary>
public class EditPostMetaFieldsTests : BaseE2ETests
{
    public EditPostMetaFieldsTests(E2EServerFixture appFixture) : base(appFixture)
    {
    }

    [E2EFact]
    public async Task EditPost_MetaFields_PersistValues()
    {
        // Arrange
        var tracker = new BrowserErrorTracker(Page);
        var client = AppFixture.GetClient(isAnonymous: false);
        var title = "Post with meta " + Guid.NewGuid();
        var note = "note-" + Guid.NewGuid();
        var body = "body-" + Guid.NewGuid();

        await AddMetaFieldsAsync(client);

        await Page.GotoAsync($"{BaseUrl}/dev/EditPost/post");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync("[data-key='note']", new() { Timeout = 5000 });

        // Act — обязательные поля поста и значения метаполей
        await FillFieldAsync(Page, "title", title);
        await FillFieldAsync(Page, "slug", "post-with-meta");
        await FillFieldAsync(Page, "note", note);
        await FillFieldAsync(Page, "body", body);

        var saveResponse = await Page.RunAndWaitForResponseAsync(
            async () => await Page.Locator("button[type='submit']").ClickAsync(),
            response => response.Url.Contains("/api/Post") && response.Request.Method == "POST",
            new() { Timeout = 10000 });

        saveResponse.Should().NotBeNull("Save API call should be made");
        saveResponse!.Status.Should().Be(201, $"Save should succeed, but got: {await saveResponse.TextAsync()}");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var created = JsonSerializer.Deserialize<PostDetailResponse>(await saveResponse.TextAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        created.Should().NotBeNull();

        // Assert — значения метаполей лежат в EAV-строках владельца (Value едет как json-значение)
        var fetched = await client.Request($"api/Post/{created!.Id}").GetJsonAsync<PostDetailResponse?>();

        fetched.Should().NotBeNull();
        fetched!.MetaValues["note"].Single().Value!.ToString().Should().Be(note);
        fetched.MetaValues["body"].Single().Value!.ToString().Should().Be(body);

        tracker.AssertNoErrors();
    }

    /// <summary>Тип поста дополняется метаполями строки и текста: сеяный «post» их не имеет</summary>
    async Task AddMetaFieldsAsync(IFlurlClient client)
    {
        var typeId = AppFixture.Catalog.PostType.Id;
        var detail = await client.Request($"api/PostType/{typeId}").GetJsonAsync<PostTypeDetailResponse>();

        var request = new UpdatePostTypeRequest
        {
            Id = detail.Id,
            Title = detail.Title,
            TypeName = detail.TypeName,
            Tags = detail.Tags,
            PostStatusList = detail.PostStatusList.Select(status => new UpdatePostStatusRequest
            {
                Id = status.Id,
                Title = status.Title,
                Slug = status.Slug,
                Color = status.Color,
                Order = status.Order,
            }).ToList(),
            EnabledFeatures = detail.EnabledFeatures,
            Disabled = detail.Disabled,
            Visibility = detail.Visibility,
            ImageFieldKey = detail.ImageFieldKey,
            MetaFields =
            [
                MetaField("note", 0, MetaFieldType.String),
                MetaField("body", 1, MetaFieldType.Text),
            ],
        };

        var response = await client.Request("api/PostType").PutJsonAsync(request);
        response.StatusCode.Should().Be(200, $"тип поста должен обновиться: {await response.GetStringAsync()}");
    }

    static UpdateMetaFieldRequest MetaField(string key, int order, MetaFieldType type) => new()
    {
        Id = Guid.NewGuid(),
        Title = key,
        Key = key,
        Type = type,
        MaxValue = null,
        MinValue = null,
        Description = "",
        IsNullable = true,
        IsMultiple = false,
        Default = null,
        Options = null,
        Order = order,
        Tags = [],
        Hidden = false,
        Disabled = false,
        ModelName = null,
        Variants = null,
    };

    /// <summary>
    /// Заполнение поля формы: настоящий input/textarea ищем внутри строки поля — Fluent-компоненты
    /// держат его в shadow DOM, а имя есть и у хоста, поэтому селектор по <c>name</c> неоднозначен.
    /// </summary>
    static async Task FillFieldAsync(IPage page, string fieldKey, string value)
    {
        var input = page.Locator($"[data-key='{fieldKey}'] input, [data-key='{fieldKey}'] textarea").First;
        await input.WaitForAsync(new() { Timeout = 5000 });
        await input.ClickAsync();
        await page.Keyboard.PressAsync("Control+a");
        await input.PressSequentiallyAsync(value, new() { Delay = 10 });
    }
}
