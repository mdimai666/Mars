using EditorJsBlazored.Blocks;

namespace EditorJsBlazored.Core;

public class EditorJsUploadFileResult
{
    public int Success { get; init; }
    public BlockImage.ImageFileData? File { get; init; } = default!;
}
