namespace AppFront.Shared.Components.FluentMarkdownSectionViews;

/// <summary>
/// Options for MarkdownSectionPreCodeRenderer
/// </summary>
internal class MarkdownSectionPreCodeRendererOptions
{
    /// <summary>
    /// html attributes for Tag element in markdig generic attributes format
    /// </summary>
    public string? PreTagAttributes;
#pragma warning disable CS0649
    /// <summary>
    /// html attributes for Code element in markdig generic attributes format
    /// </summary>
    public string? CodeTagAttributes;
#pragma warning restore CS0649
}
