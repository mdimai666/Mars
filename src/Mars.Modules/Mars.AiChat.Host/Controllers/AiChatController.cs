using System.Net.Mime;
using Mars.AiChat.Host.Shared.Interfaces;
using Mars.AiChat.Host.Shared.Models;
using Mars.AiChat.Shared.Dto;
using Mars.Core.Exceptions;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Features;
using Mars.Host.Shared.Interfaces;
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
    private readonly IAiChatSessionStore _store;
    private readonly IAiChatRunCoordinator _coordinator;
    private readonly IRequestContext _requestContext;

    public AiChatController(
        IAiChatSessionStore store,
        IAiChatRunCoordinator coordinator,
        IRequestContext requestContext)
    {
        _store = store;
        _coordinator = coordinator;
        _requestContext = requestContext;
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
        var now = DateTime.UtcNow;
        var state = new AiChatSessionState
        {
            Id = Guid.NewGuid(),
            UserId = GetUserId(),
            Title = string.IsNullOrWhiteSpace(request?.Title) ? "Новый чат" : request!.Title!.Trim(),
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

    [HttpDelete("sessions/{chatId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task DeleteSession(Guid chatId, CancellationToken cancellationToken)
    {
        _coordinator.Stop(chatId);
        await _store.DeleteAsync(chatId, GetUserId(), cancellationToken);
    }

    /// <summary>
    /// Отправляет сообщение агенту. Ответ приходит событиями SignalR хаба /_ws/aichat.
    /// </summary>
    [HttpPost("sessions/{chatId:guid}/send")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Send(Guid chatId, [FromBody] AiChatSendRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new UserActionException("Пустое сообщение");

        var userId = GetUserId();
        _ = await _store.GetAsync(chatId, userId, cancellationToken)
            ?? throw new NotFoundException($"AiChat session '{chatId}' not found");

        _coordinator.Enqueue(chatId, userId, request.Message.Trim(), request.PageContext);

        return new AcceptedResult();
    }

    [HttpPost("sessions/{chatId:guid}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Stop(Guid chatId)
    {
        return new OkObjectResult(new { Stopped = _coordinator.Stop(chatId) });
    }
}
