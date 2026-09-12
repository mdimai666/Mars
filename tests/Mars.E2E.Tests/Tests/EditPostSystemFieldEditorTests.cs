using System.Text.Json;
using FluentAssertions;
using Flurl.Http;
using Mars.Cms.Contracts.PostTypes;
using Mars.Cms.Contracts.Posts;
using Mars.E2E.Tests.Fixtures;
using Mars.E2E.Tests.Helpers;
using Mars.Forms.Contracts;
using Mars.Integration.Tests.Attributes;
using Microsoft.Playwright;

namespace Mars.E2E.Tests.Tests;

/// <summary>
/// Редактор значения выбирается для любого поля, а не только для контента: системному слоту
/// <c>title</c> ставим редактор «Цвет» и проверяем, что форма его рисует и сохраняет значение.
/// </summary>
public class EditPostSystemFieldEditorTests : BaseE2ETests
{
    public EditPostSystemFieldEditorTests(E2EServerFixture appFixture) : base(appFixture)
    {
    }

    [E2EFact]
    public async Task EditPost_TitleSlot_WithColorEditor_RendersAndSaves()
    {
        // Arrange
        var tracker = new BrowserErrorTracker(Page);
        var client = AppFixture.GetClient(isAnonymous: false);
        var color = "#3366cc";

        await SetTitleEditorAsync(client, FormEditorCatalog.Color);

        await Page.GotoAsync($"{BaseUrl}/dev/EditPost/post");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync("[data-key='title'] input[type='color']", new() { Timeout = 5000 });

        // Act — значение правится текстовым полем рядом с палитрой, запись — по расфокусу
        await FillFieldAsync(Page, "title", color);
        await FillFieldAsync(Page, "slug", "title-as-color");

        var saveResponse = await Page.RunAndWaitForResponseAsync(
            async () => await Page.Locator("button[type='submit']").ClickAsync(),
            response => response.Url.Contains("/api/Post") && response.Request.Method == "POST",
            new() { Timeout = 10000 });

        saveResponse.Should().NotBeNull("Save API call should be made");
        saveResponse!.Status.Should().Be(201, $"Save should succeed, but got: {await saveResponse.TextAsync()}");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert — значение редактора ушло в колонку поста
        var created = JsonSerializer.Deserialize<PostDetailResponse>(await saveResponse.TextAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        created.Should().NotBeNull();

        var fetched = await client.Request($"api/Post/{created!.Id}").GetJsonAsync<PostDetailResponse?>();

        fetched!.Title.Should().Be(color);

        tracker.AssertNoErrors();
    }

    /// <summary>Слот <c>title</c> получает редактор «Цвет» — параметры системных полей типа</summary>
    async Task SetTitleEditorAsync(IFlurlClient client, string editorKey)
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
            MetaFields = [],
            SystemFields = [new FormFieldSettings { Key = SystemFieldsCatalog.Title, Editor = editorKey }],
        };

        var response = await client.Request("api/PostType").PutJsonAsync(request);
        response.StatusCode.Should().Be(200, $"тип поста должен обновиться: {await response.GetStringAsync()}");
    }

    /// <summary>
    /// Заполнение поля формы: текстовый input/textarea внутри строки поля (значение пишется
    /// по расфокусу). Селектор по <c>name</c> не годится — имя есть и у хоста Fluent-компонента,
    /// а первым в строке редактора цвета идёт палитра.
    /// </summary>
    static async Task FillFieldAsync(IPage page, string fieldKey, string value)
    {
        var input = page.Locator($"[data-key='{fieldKey}'] input[type='text'], [data-key='{fieldKey}'] textarea").First;
        await input.WaitForAsync(new() { Timeout = 5000 });
        await input.ClickAsync();
        await page.Keyboard.PressAsync("Control+a");
        await input.PressSequentiallyAsync(value, new() { Delay = 10 });
    }
}
