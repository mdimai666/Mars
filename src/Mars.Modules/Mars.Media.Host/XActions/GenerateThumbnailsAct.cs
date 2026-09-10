using Mars.Media.Abstractions.Services;
using Mars.XActions.Contracts;

namespace Mars.Media.Host.XActions;

/// <summary>
/// Перегенерирует миниатюры и метаданные изображений.
/// </summary>
public class GenerateThumbnailsAct(IMediaService mediaService) : IAct
{
    public const string CommandId = "mars.media.generateThumbnails";

    public async Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        var result = await mediaService.GenerateThumbnails(false, cancellationToken);
        return result.Ok ? XActResult.ToastSuccess(result.Message) : XActResult.ToastError(result.Message);
    }
}
