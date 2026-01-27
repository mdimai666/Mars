namespace EditorJsBlazored.Host.Dto;

public sealed class LinkToolPreviewResult
{
    public int Success { get; init; } = 1;
    public LinkToolPreviewMeta Meta { get; init; } = default!;
}

public sealed class LinkToolPreviewMeta
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public LinkToolPreviewImage? Image { get; init; }
}

public sealed class LinkToolPreviewImage
{
    public string Url { get; init; } = default!;
}
