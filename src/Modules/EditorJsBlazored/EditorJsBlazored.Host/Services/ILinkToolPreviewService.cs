using EditorJsBlazored.Host.Dto;

namespace EditorJsBlazored.Host.Services;

public interface ILinkToolPreviewService
{
    Task<LinkToolPreviewResult?> GetPreviewAsync(string url, CancellationToken ct = default);
}
