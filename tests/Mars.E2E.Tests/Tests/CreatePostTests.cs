using System.Text.Json;
using FluentAssertions;
using Flurl.Http;
using Mars.E2E.Tests.Fixtures;
using Mars.E2E.Tests.Helpers;
using Mars.Integration.Tests.Attributes;
using Mars.Cms.Contracts.Posts;
using Microsoft.Playwright;

namespace Mars.E2E.Tests.Tests;

public class CreatePostTests : BaseE2ETests
{
    public CreatePostTests(E2EServerFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact(Skip = SkipE2ETests)]
    public async Task CreatePost_WithRequiredFields_ShouldPersist()
    {
        // Arrange
        var tracker = new BrowserErrorTracker(Page);
        var title = "Test Post Title";
        var slug = "test-post-title";
        var contentGuid = Guid.NewGuid().ToString();
        var tags = new[] { "test-tag-1", "test-tag-2" };

        await Page.GotoAsync($"{BaseUrl}/dev/EditPost/post");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync("[name='Title']", new() { Timeout = 3000 });

        // Act — fill required fields
        await FillTextField(Page, "Title", title);
        await Task.Delay(500);
        await FillTextField(Page, "Slug", slug);

        // Fill Content via BlockEditor (Editor.js)
        await FillBlockEditorContent(Page, contentGuid);
        await Task.Delay(1000); // Wait for Editor.js to sync content with Blazor

        // Fill Tags
        await FillTags(Page, tags);

        // Save and verify API response
        var saveResponse = await Page.RunAndWaitForResponseAsync(
            async () => await Page.Locator("button[type='submit']").ClickAsync(),
            response => response.Url.Contains("/api/Post") && response.Request.Method == "POST",
            new() { Timeout = 3000 });

        saveResponse.Should().NotBeNull("Save API call should be made");
        saveResponse!.Status.Should().Be(201, $"Save should succeed, but got: {await saveResponse.TextAsync()}");

        // Wait for all network requests to complete
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Get created post ID from response
        var responseText = await saveResponse.TextAsync();
        var createdPost = JsonSerializer.Deserialize<PostDetailResponse>(responseText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        createdPost.Should().NotBeNull();
        createdPost!.Id.Should().NotBeEmpty();

        // Assert — verify post via API
        var client = AppFixture.GetClient(isAnonymous: false);
        var fetchedPost = await client.Request($"api/Post/{createdPost.Id}").GetJsonAsync<PostDetailResponse?>();

        fetchedPost.Should().NotBeNull();
        fetchedPost!.Title.Should().Be(title);
        fetchedPost.Slug.Should().Be(slug);
        fetchedPost.Type.Should().Be("post");
        fetchedPost.Content.Should().Contain(contentGuid);
        fetchedPost.Tags.Should().BeEquivalentTo(tags);

        tracker.AssertNoErrors();
    }

    /// <summary>
    /// Fills a Fluent UI text field by clicking, selecting all, and typing new value.
    /// </summary>
    private static async Task FillTextField(IPage page, string fieldName, string value)
    {
        await page.Locator($"[name='{fieldName}']").ClickAsync();
        await page.Keyboard.PressAsync("Control+a");
        await page.Locator($"[name='{fieldName}']").PressSequentiallyAsync(value, new() { Delay = 10 });
    }

    /// <summary>
    /// Fills BlockEditor (Editor.js) content by using Editor.js API to add a paragraph block.
    /// </summary>
    private static async Task FillBlockEditorContent(IPage page, string content)
    {
        // Wait for BlockEditor to initialize
        await page.WaitForSelectorAsync(".BlockEditor1", new() { Timeout = 3000 });
        await Task.Delay(1000); // Wait for Editor.js to fully initialize

        // Use Editor.js API to add a paragraph block
        // Editor.js instances are stored in window.editorJsHandler.editors
        await page.EvaluateAsync(@"(content) => {
            const editors = window.editorJsHandler.editors;
            const editorKeys = Object.keys(editors);
            if (editorKeys.length === 0) {
                throw new Error('No Editor.js instances found');
            }
            
            // Get the first editor instance
            const editorWrapper = editors[editorKeys[0]];
            const editorJs = editorWrapper.editorJsInstance;
            
            // Add a paragraph block with the content
            editorJs.blocks.insert('paragraph', { text: content });
        }", content);
        
        // Wait for Editor.js to process the change
        await Task.Delay(500);
    }

    /// <summary>
    /// Fills tags using InputTags2 component by typing and pressing Enter.
    /// </summary>
    private static async Task FillTags(IPage page, string[] tags)
    {
        // Find the input inside input-tag2 component (fluent-text-field renders as web component)
        var tagInput = page.Locator("input-tag2 fluent-text-field input").First;
        await tagInput.WaitForAsync(new() { Timeout = 3000 });

        foreach (var tag in tags)
        {
            await tagInput.ClickAsync();
            await tagInput.FillAsync(tag);
            await tagInput.PressAsync("Enter");
            await Task.Delay(300);
        }
    }
}
