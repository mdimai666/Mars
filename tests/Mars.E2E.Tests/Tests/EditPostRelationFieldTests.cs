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
/// Связь метаполя в форме поста: цель связи и кратность приезжают дескриптором поля, значение
/// выбирается общим пикером и сохраняется строкой EAV (ModelId).
/// </summary>
public class EditPostRelationFieldTests : BaseE2ETests
{
    public EditPostRelationFieldTests(E2EServerFixture appFixture) : base(appFixture)
    {
    }

    [E2EFact]
    public async Task EditPost_RelationMetaField_PersistsSelectedModel()
    {
        // Arrange — цель связи: свежий пост того же типа (в пикере список отсортирован по дате убыв.)
        var tracker = new BrowserErrorTracker(Page);
        var client = AppFixture.GetClient(isAnonymous: false);
        var postType = AppFixture.Catalog.PostType.TypeName;
        var targetTitle = "Relation target " + Guid.NewGuid();
        var postTitle = "Post with relation " + Guid.NewGuid();

        var targetResponse = await client.Request("api/Post").PostJsonAsync(new CreatePostRequest
        {
            Id = null,
            Title = targetTitle,
            Type = postType,
            Slug = "relation-target-" + Guid.NewGuid().ToString("N")[..8],
            Tags = [],
            Content = null,
            Status = null,
            Excerpt = null,
            LangCode = "",
            CategoryIds = [],
            MetaValues = [],
        });
        targetResponse.StatusCode.Should().Be(201, $"цель связи должна создаться: {await targetResponse.GetStringAsync()}");
        var target = await targetResponse.GetJsonAsync<PostDetailResponse>();

        await AddRelationFieldAsync(client, $"Post.{postType}");

        await Page.GotoAsync($"{BaseUrl}/dev/EditPost/{postType}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync("[data-key='related']", new() { Timeout = 5000 });

        var field = Page.Locator("[data-key='related']");
        var selectButton = field.Locator("fluent-button:has-text('Выбрать')").First;
        await selectButton.WaitForAsync(new() { Timeout = 5000 });

        // Act — выбираем цель общим пикером связи и сохраняем пост
        await selectButton.ClickAsync();

        var firstRow = Page.Locator(".d-metavalue-relation-select-dialog__list-item").First;
        await firstRow.WaitForAsync(new() { Timeout = 10000 });
        await firstRow.ClickAsync();

        await field.Locator($"text={targetTitle}").First.WaitForAsync(new() { Timeout = 10000 });

        await FillFieldAsync(Page, "title", postTitle);
        await FillFieldAsync(Page, "slug", "post-with-relation");

        var saveResponse = await Page.RunAndWaitForResponseAsync(
            async () => await Page.Locator("button[type='submit']").ClickAsync(),
            response => response.Url.Contains("/api/Post") && response.Request.Method == "POST",
            new() { Timeout = 10000 });

        saveResponse.Should().NotBeNull("Save API call should be made");
        saveResponse!.Status.Should().Be(201, $"Save should succeed, but got: {await saveResponse.TextAsync()}");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert — выбранная цель лежит строкой EAV владельца
        var created = JsonSerializer.Deserialize<PostDetailResponse>(await saveResponse.TextAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        created.Should().NotBeNull();

        var fetched = await client.Request($"api/Post/{created!.Id}").GetJsonAsync<PostDetailResponse?>();

        fetched.Should().NotBeNull();
        fetched!.MetaValues.Should().ContainKey("related");
        fetched.MetaValues["related"].Single().Value!.ToString().Should().Be(target!.Id.ToString());

        tracker.AssertNoErrors();
    }

    /// <summary>Тип поста дополняется полем-связью на пост того же типа</summary>
    async Task AddRelationFieldAsync(IFlurlClient client, string modelName)
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
                new UpdateMetaFieldRequest
                {
                    Id = Guid.NewGuid(),
                    Title = "Связанный пост",
                    Key = "related",
                    Type = MetaFieldType.Relation,
                    MaxValue = null,
                    MinValue = null,
                    Description = "",
                    IsNullable = true,
                    IsMultiple = false,
                    Default = null,
                    Options = null,
                    Order = 0,
                    Tags = [],
                    Hidden = false,
                    Disabled = false,
                    ModelName = modelName,
                    Variants = null,
                },
            ],
        };

        var response = await client.Request("api/PostType").PutJsonAsync(request);
        response.StatusCode.Should().Be(200, $"тип поста должен обновиться: {await response.GetStringAsync()}");
    }

    /// <summary>Заполнение поля формы: настоящий input/textarea внутри строки поля</summary>
    static async Task FillFieldAsync(IPage page, string fieldKey, string value)
    {
        var input = page.Locator($"[data-key='{fieldKey}'] input, [data-key='{fieldKey}'] textarea").First;
        await input.WaitForAsync(new() { Timeout = 5000 });
        await input.ClickAsync();
        await page.Keyboard.PressAsync("Control+a");
        await input.PressSequentiallyAsync(value, new() { Delay = 10 });
    }
}
