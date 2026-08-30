using System.Net.Mime;
using Mars.AiChat.Abstractions.Interfaces;
using Mars.AiChat.Abstractions.Models;
using Mars.AiChat.Contracts.Dto;
using Mars.AiChat.Contracts.Options;
using Mars.Core.Exceptions;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Services;
using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.Server.Abstractions.Features;
using Mars.Server.Abstractions.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace Mars.AiChat.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
[FeatureGate(FeatureFlags.AiChat)]
public class AiChatController
{
    /// <summary>Максимальный размер одного вложения чата (32 МБ).</summary>
    private const int MaxAttachmentBytes = 32 * 1024 * 1024;

    private readonly IAiChatSessionStore _store;
    private readonly IAiChatRunCoordinator _coordinator;
    private readonly IRequestContext _requestContext;
    private readonly IFileService _fileService;
    private readonly IValidatorFactory _validatorFactory;
    private readonly IOptionService _optionService;

    public AiChatController(
        IAiChatSessionStore store,
        IAiChatRunCoordinator coordinator,
        IRequestContext requestContext,
        IFileService fileService,
        IValidatorFactory validatorFactory,
        IOptionService optionService)
    {
        _store = store;
        _coordinator = coordinator;
        _requestContext = requestContext;
        _fileService = fileService;
        _validatorFactory = validatorFactory;
        _optionService = optionService;
    }

    private Guid GetUserId()
        => _requestContext.User?.Id ?? throw new UserActionException("Пользователь не определён");

    [HttpGet("sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<AiChatSessionSummary>> GetSessions(CancellationToken cancellationToken)
    {
        var list = await _store.ListAsync(GetUserId(), cancellationToken);

        foreach (var summary in list)
            summary.IsRunning = _coordinator.IsRunning(summary.Id);

        return list;
    }

    [HttpPost("sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<AiChatSessionSummary> CreateSession([FromBody] AiChatCreateSessionRequest? request, CancellationToken cancellationToken)
    {
        // подключение нового чата: наследуется из запроса, неизвестное имя тихо становится дефолтом
        var connectionName = request?.ConnectionName?.Trim();
        if (!string.IsNullOrEmpty(connectionName))
        {
            var option = _optionService.GetOption<AiChatOption>();
            if (!option.Connections.Any(c => c.Name == connectionName))
                connectionName = null;
        }

        var now = DateTime.UtcNow;
        var state = new AiChatSessionState
        {
            Id = Guid.NewGuid(),
            UserId = GetUserId(),
            Title = string.IsNullOrWhiteSpace(request?.Title) ? "Новый чат" : request!.Title!.Trim(),
            ConnectionName = string.IsNullOrEmpty(connectionName) ? null : connectionName,
            CreatedAtUtc = now,
            ModifiedAtUtc = now,
        };

        await _store.SaveAsync(state, cancellationToken);

        return state.ToSummary(isRunning: false);
    }

    [HttpGet("sessions/{chatId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<AiChatSessionDto> GetSession(Guid chatId, CancellationToken cancellationToken)
    {
        var state = await _store.GetAsync(chatId, GetUserId(), cancellationToken)
            ?? throw new NotFoundException($"AiChat session '{chatId}' not found");

        return state.ToDto(_coordinator.IsRunning(chatId));
    }

    /// <summary>
    /// Настроенные подключения к ИИ-сервисам (без секретов). Пустой список — ИИ не настроен.
    /// </summary>
    [HttpGet("connections")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IReadOnlyList<AiChatConnectionDto> GetConnections()
    {
        var option = _optionService.GetOption<AiChatOption>();
        return option.Connections.Select(c => new AiChatConnectionDto
        {
            Name = c.Name,
            ProviderType = c.ProviderType,
            ModelId = c.ModelId,
            IsDefault = option.GetDefaultConnection()?.Name == c.Name,
        }).ToList();
    }

    /// <summary>
    /// Выбирает подключение (модель) для чата. Пустое имя — вернуть подключение по умолчанию.
    /// </summary>
    [HttpPut("sessions/{chatId:guid}/connection")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<AiChatSessionDto> SetConnection(Guid chatId, [FromBody] AiChatSetConnectionRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var state = await _store.GetAsync(chatId, userId, cancellationToken)
            ?? throw new NotFoundException($"AiChat session '{chatId}' not found");

        var name = request.ConnectionName?.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            var option = _optionService.GetOption<AiChatOption>();
            if (!option.Connections.Any(c => c.Name == name))
                throw new UserActionException($"Подключение «{name}» не найдено в настройках ИИ-чата");
        }

        state.ConnectionName = string.IsNullOrEmpty(name) ? null : name;
        state.ModifiedAtUtc = DateTime.UtcNow;
        await _store.SaveAsync(state, cancellationToken);

        return state.ToDto(_coordinator.IsRunning(chatId));
    }

    [HttpDelete("sessions/{chatId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task DeleteSession(Guid chatId, CancellationToken cancellationToken)
    {
        _coordinator.Stop(chatId);
        await _store.DeleteAsync(chatId, GetUserId(), cancellationToken);
    }

    /// <summary>
    /// Загружает файл для сообщения чата в медиатеку (Media/AiChat/{год}) и возвращает его данные.
    /// Файл становится видимым на странице «Медиа» и доступным агенту.
    /// </summary>
    [HttpPost("attachments")]
    [RequestSizeLimit(MaxAttachmentBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<AiChatAttachmentDto> UploadAttachment(IFormFile file, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync<IFormFile, UploadMediaFileValidator>(file, cancellationToken);

        var subpath = $"Media/AiChat/{DateTimeOffset.Now.Year}";
        var fileId = await _fileService.WriteUpload(file, subpath, GetUserId(), cancellationToken);
        var detail = await _fileService.GetDetail(fileId, cancellationToken)
            ?? throw new NotFoundException($"File '{fileId}' not found");

        return ToAttachment(detail);
    }

    /// <summary>
    /// Отправляет сообщение агенту. Ответ приходит событиями SignalR хаба /_ws/aichat.
    /// </summary>
    [HttpPost("sessions/{chatId:guid}/send")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Send(Guid chatId, [FromBody] AiChatSendRequest request, CancellationToken cancellationToken)
    {
        var attachmentIds = request.AttachmentIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList() ?? [];

        if (string.IsNullOrWhiteSpace(request.Message) && attachmentIds.Count == 0)
            throw new UserActionException("Пустое сообщение");

        var userId = GetUserId();
        _ = await _store.GetAsync(chatId, userId, cancellationToken)
            ?? throw new NotFoundException($"AiChat session '{chatId}' not found");

        _coordinator.Enqueue(chatId, userId, request.Message.Trim(), request.PageContext,
            attachmentIds.Count > 0 ? attachmentIds : null);

        return new AcceptedResult();
    }

    private static AiChatAttachmentDto ToAttachment(FileSummary file) => new()
    {
        FileId = file.Id,
        Name = file.Name,
        Ext = file.Ext,
        Size = file.Size,
        IsImage = file.IsImage,
        UrlRelative = file.UrlRelative,
    };

    [HttpPost("sessions/{chatId:guid}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Stop(Guid chatId)
    {
        return new OkObjectResult(new { Stopped = _coordinator.Stop(chatId) });
    }
}
