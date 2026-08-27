namespace Mars.Shared.Utils;

/// <summary>
/// Represents the scroll position and dimensions of an element.
/// </summary>
public readonly record struct ScrollInfo
{
    /// <summary>Gets the number of pixels that an element's content is scrolled vertically.</summary>
    public int ScrollTop { get; init; }

    /// <summary>Gets the number of pixels that an element's content is scrolled horizontally.</summary>
    public int ScrollLeft { get; init; }

    /// <summary>Gets the total height of an element's content, including content not visible on the screen.</summary>
    public int ScrollHeight { get; init; }

    /// <summary>Gets the inner height of an element in pixels, including padding but excluding borders and scrollbars.</summary>
    public int ClientHeight { get; init; }

    /// <summary>Gets the total width of an element's content, including content not visible on the screen.</summary>
    public int ScrollWidth { get; init; }

    /// <summary>Gets the inner width of an element in pixels, including padding but excluding borders and scrollbars.</summary>
    public int ClientWidth { get; init; }

    /// <summary>
    /// Gets a value indicating whether the element is scrolled to the very bottom.
    /// Includes a 1-pixel tolerance for subpixel rendering layouts.
    /// </summary>
    public bool IsAtBottom =>
        ScrollTop + ClientHeight >= ScrollHeight - 1;
}
