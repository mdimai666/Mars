using Mars.AiChat.Host.Tools;
using Mars.Media.Abstractions.Services;
using Mars.Server.Abstractions.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mars.AiChat.Host.Toolsets;

/// <summary>
/// Медиатека: список/детали/добавление/удаление файлов. Включён всегда —
/// медиа доступна на любой странице, правила работы заданы в скилле mars-media.
/// </summary>
public class MediaToolset : IAiToolset
{
    private readonly IFileService _fileService;
    private readonly IFileStorage _fileStorage;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public MediaToolset(
        IFileService fileService,
        IFileStorage fileStorage,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _fileService = fileService;
        _fileStorage = fileStorage;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    public string Name => "media";

    public IReadOnlyList<AIFunction> Build(AiToolsetContext ctx)
    {
        var tools = new MarsMediaTools(_fileService, _fileStorage, _httpClientFactory, _loggerFactory.CreateLogger<MarsMediaTools>(), ctx.UserId);
        return
        [
            AIFunctionFactory.Create(tools.ListMedia),
            AIFunctionFactory.Create(tools.GetMedia),
            AIFunctionFactory.Create(tools.ReadMediaFile),
            AIFunctionFactory.Create(tools.AddMedia),
            AIFunctionFactory.Create(tools.DeleteMedia),
        ];
    }
}
