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
/// Раскладка формы со структурой: два контейнера-таба, ряд из двух колонок (одна пустая),
/// заголовок и разделитель. Проверяется, что форма рисует сетку и табы, а поле из колонки
/// сохраняется. Раскладка ставится через API презентации типа.
/// </summary>
public class EditPostLayoutGridTests : BaseE2ETests
{
    public EditPostLayoutGridTests(E2EServerFixture appFixture) : base(appFixture)
    {
    }

    [E2EFact]
    public async Task EditPost_ContainerTabsAndColumns_RenderAndSave()
    {
        // Arrange
        var tracker = new BrowserErrorTracker(Page);
        var client = AppFixture.GetClient(isAnonymous: false);
        var title = "Grid post " + Guid.NewGuid();

        var tab = Key("tab");
        var secondTab = Key("tab");
        var row = Key("row");
        var left = Key("col");
        var right = Key("col");
        var heading = Key("heading");
        var divider = Key("divider");

        await SetFormLayoutAsync(client,
        [
            new FormItem { Key = tab, Kind = FormItemKind.Container, Zone = SystemFieldsCatalog.Zones.Main, Title = "Основное" },
            new FormItem { Key = row, Kind = FormItemKind.Row, Parent = tab },
            new FormItem { Key = left, Kind = FormItemKind.Column, Parent = row, Width = FormItemWidths.Half },
            new FormItem { Key = "title", Parent = left },
            new FormItem { Key = right, Kind = FormItemKind.Column, Parent = row, Width = FormItemWidths.Half },
            new FormItem { Key = "slug", Parent = tab },
            new FormItem { Key = heading, Kind = FormItemKind.Heading, Parent = tab, Title = "Заголовок раскладки" },
            new FormItem { Key = divider, Kind = FormItemKind.Divider, Parent = tab },
            new FormItem { Key = secondTab, Kind = FormItemKind.Container, Zone = SystemFieldsCatalog.Zones.Main, Title = "Дополнительно" },
        ]);

        await Page.GotoAsync($"{BaseUrl}/dev/EditPost/post");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync("[data-key='title'] input", new() { Timeout = 5000 });

        // Assert — контейнеров два, значит появились табы; поле стоит в колонке половины ширины
        (await Page.Locator("fluent-tab", new() { HasTextString = "Основное" }).CountAsync())
            .Should().BeGreaterThan(0, "контейнеры рисуются табами");
        (await Page.Locator("fluent-tab", new() { HasTextString = "Дополнительно" }).CountAsync())
            .Should().BeGreaterThan(0);
        (await Page.Locator($".col-md-6 [data-key='title']").CountAsync())
            .Should().BeGreaterThan(0, "поле лежит в колонке на половину ширины");
        (await Page.Locator("h6", new() { HasTextString = "Заголовок раскладки" }).CountAsync())
            .Should().BeGreaterThan(0);
        (await Page.Locator("hr").CountAsync()).Should().BeGreaterThan(0, "разделитель — неполевой элемент");

        // Act — значения полей формы
        await FillFieldAsync(Page, "title", title);
        await FillFieldAsync(Page, "slug", "grid-post");

        var saveResponse = await Page.RunAndWaitForResponseAsync(
            async () => await Page.Locator("button[type='submit']").ClickAsync(),
            response => response.Url.Contains("/api/Post") && response.Request.Method == "POST",
            new() { Timeout = 10000 });

        saveResponse.Should().NotBeNull("Save API call should be made");
        saveResponse!.Status.Should().Be(201, $"Save should succeed, but got: {await saveResponse.TextAsync()}");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert — поле из колонки сохранилось в свою колонку поста
        var created = JsonSerializer.Deserialize<PostDetailResponse>(await saveResponse.TextAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        created.Should().NotBeNull();

        var fetched = await client.Request($"api/Post/{created!.Id}").GetJsonAsync<PostDetailResponse?>();
        fetched!.Title.Should().Be(title);

        tracker.AssertNoErrors();
    }

    /// <summary>Раскладка формы ставится через презентацию типа; остальные настройки сохраняются как есть</summary>
    async Task SetFormLayoutAsync(IFlurlClient client, IReadOnlyCollection<FormItem> items)
    {
        var typeId = AppFixture.Catalog.PostType.Id;
        var model = await client.Request($"api/PostType/presentation/edit/{typeId}")
                                 .GetJsonAsync<PostTypePresentationEditViewModel>();

        var request = new UpdatePostTypePresentationRequest
        {
            Id = typeId,
            ListViewTemplate = model.Presentation.ListViewTemplate ?? "",
            Grid = model.Presentation.Grid,
            Form = new FormLayoutSettings { Items = items },
        };

        var response = await client.Request("api/PostType/presentation/update").PutJsonAsync(request);
        response.StatusCode.Should().Be(200, $"раскладка должна сохраниться: {await response.GetStringAsync()}");
    }

    static string Key(string prefix) => $"{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    /// <summary>
    /// Заполнение поля формы: настоящий input ищем внутри строки поля — Fluent-компоненты держат
    /// его в shadow DOM, а имя есть и у хоста, поэтому селектор по <c>name</c> неоднозначен.
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
