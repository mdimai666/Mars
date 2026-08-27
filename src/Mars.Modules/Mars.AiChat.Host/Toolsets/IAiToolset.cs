using Mars.AiChat.Host.Tools;
using Mars.AiChat.Contracts.Options;
using Microsoft.Extensions.AI;

namespace Mars.AiChat.Host.Toolsets;

/// <summary>
/// Контекст одного запуска агента: тулсеты по нему решают, релевантны ли они,
/// и строят свои инструменты (в т.ч. per-run экземпляры).
/// </summary>
public sealed record AiToolsetContext(
    Guid UserId,
    Guid ChatId,
    AiChatOption Option,
    string? PageContext,
    string? FrontEditorSlug,
    AskUserTool AskUser,
    bool SkillsEnabled);

/// <summary>
/// Набор инструментов агента по домену. Новый домен = новый класс + регистрация в DI,
/// AiChatAgentService не меняется. Инструменты собираются на каждый запуск.
/// </summary>
public interface IAiToolset
{
    string Name { get; }

    bool IsEnabled(AiToolsetContext ctx) => true;

    IReadOnlyList<AIFunction> Build(AiToolsetContext ctx);
}
