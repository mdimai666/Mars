using Mars.Contracts.XActions;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Media.Abstractions.Services;

namespace Mars.Media.Host.XActions;

/// <summary>
/// Сканирует файловое хранилище медиа и регистрирует найденные файлы в БД.
/// </summary>
public class ScanMediaFilesAct(
    IMediaService mediaService,
    IRequestContext requestContext) : IAct
{
    public const string CommandId = "mars.media.scanFiles";

    public async Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        var result = await mediaService.ScanFilesAndSaveInDB(requestContext.User.Id, cancellationToken);
        return result.Ok ? XActResult.ToastSuccess(result.Message) : XActResult.ToastError(result.Message);
    }
}
