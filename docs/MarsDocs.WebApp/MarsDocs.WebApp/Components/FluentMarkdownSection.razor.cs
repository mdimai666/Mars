using System.Net;
using Markdig;
using Markdig.Extensions.AutoLinks;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarsDocs.WebApp.Components.FluentMarkdownSectionViews;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MarsDocs.WebApp.Components;

public partial class FluentMarkdownSection : FluentComponentBase
{
    private IJSObjectReference _jsModule = default!;
    private bool _markdownChanged = false;
    private string? _content;
    private string? _fromAsset;

    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = default!;

    //[Inject]
    //private IStaticAssetService StaticAssetService { get; set; } = default!;

    [Inject]
    protected HttpClient client { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Markdown content
    /// </summary>
    [Parameter]
    public string? Content
    {
        get => _content;
        set
        {
            if (_content is not null && !_content.Equals(value))
            {
                _markdownChanged = true;
            }
            _content = value;
        }
    }

    /// <summary>
    /// Gets or sets asset to read the Markdown from
    /// </summary>
    [Parameter]
    public string? FromAsset
    {
        get => _fromAsset;
        set
        {
            if (_fromAsset is not null && !_fromAsset.Equals(value))
            {
                _markdownChanged = true;
            }
            _fromAsset = value;
        }
    }

    [Parameter]
    public EventCallback OnContentConverted { get; set; }

    public MarkupString HtmlContent { get; private set; }

    protected override void OnInitialized()
    {
        if (Content is null && string.IsNullOrEmpty(FromAsset))
        {
            throw new ArgumentException("You need to provide either Content or FromAsset parameter");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // import code for highlighting code blocks
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import",
                "./Components/FluentMarkdownSection.razor.js");
        }

        if (firstRender || _markdownChanged)
        {
            _markdownChanged = false;

            // create markup from markdown source
            HtmlContent = await MarkdownToMarkupStringAsync();
            StateHasChanged();
            await Task.Delay(10);

            // notify that content converted from markdown
            if (OnContentConverted.HasDelegate)
            {
                await OnContentConverted.InvokeAsync();
            }
            await _jsModule.InvokeVoidAsync("highlight");
            await _jsModule.InvokeVoidAsync("addCopyButton");
        }
    }

    /// <summary>
    /// Converts markdown, provided in Content or from markdown file stored as a static asset, to MarkupString for rendering.
    /// </summary>
    /// <returns>MarkupString</returns>
    private async Task<MarkupString> MarkdownToMarkupStringAsync()
    {
        string? markdown = null;

        if (string.IsNullOrEmpty(FromAsset))
        {
            markdown = Content;
        }
        else
        {
            //markdown = await StaticAssetService.GetAsync(FromAsset);

            try
            {
                markdown = await client.GetStringAsync(FromAsset);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                markdown = $"<div class=\"alert alert-warning\" role=\"alert\">Asset '{FromAsset}' not found</div>";
            }
        }

        return ConvertToMarkupString(markdown);
    }
    private static MarkupString ConvertToMarkupString(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var builder = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .UseAutoLinks(new AutoLinkOptions { OpenInNewWindow = true })
                    .Use<MarkdownSectionPreCodeExtension>();

            var pipeline = builder.Build();

            // Convert markdown string to HTML
            //var html = Markdown.ToHtml(value, pipeline);
            MarkdownDocument document = Markdown.Parse(value, pipeline);

            foreach (LinkInline link in document.Descendants<LinkInline>())
            {
                if (link.Url.Contains("://"))
                    link.GetAttributes().AddPropertyIfNotExist("target", "_blank");
            }

            foreach (AutolinkInline link in document.Descendants<AutolinkInline>())
            {
                if (link.Url.Contains("://"))
                    link.GetAttributes().AddPropertyIfNotExist("target", "_blank");
            }

            string html = document.ToHtml(pipeline);

            // Return sanitized HTML as a MarkupString that Blazor can render
            return new MarkupString(html);
        }

        return new MarkupString();
    }
}
