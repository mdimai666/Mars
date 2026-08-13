using System.CommandLine;
using Mars.AiChat.Host.Services;
using Mars.AiChat.Host.Shared.Interfaces;
using Mars.AiChat.Host.Shared.Models;
using Mars.AiChat.Shared.Dto;
using Mars.Host.Shared.CommandLine;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.AiChat.Host.CommandLine;

/// <summary>
/// Полигон: разовый прогон агента без веб-сервера и браузера.
/// mars aichat send -m "задача" [-p /front/editor/landing] [-u userId]
/// Печатает транскрипт (вызовы инструментов, результаты, ответ) в консоль.
/// </summary>
public class AiChatCli : CommandCli
{
    public AiChatCli(ICommandLineApi cli) : base(cli)
    {
        var aichat = new Command("aichat", "AI chat agent (headless playground)");

        var optionMessage = new Option<string>("--message", "-m") { Description = "user message", Required = true };
        var optionPage = new Option<string?>("--page", "-p") { Description = "page context, e.g. /front/editor/landing" };
        var optionUser = new Option<Guid?>("--user", "-u") { Description = "user id (default: first admin by creation date)" };
        var optionSkills = new Option<bool?>("--skills") { Description = "A/B: skills toolset (SearchSkills/LoadSkill) on/off, default on" };
        var optionAccess = new Option<bool?>("--access") { Description = "A/B: agent workspace (file_access_*) on/off, default on" };

        var send = new Command("send", "create a fresh chat, run the agent once, print the transcript")
        {
            optionMessage, optionPage, optionUser, optionSkills, optionAccess,
        };
        send.SetAction((p, ct) => SendCommand(
            p.GetRequiredValue(optionMessage), p.GetValue(optionPage), p.GetValue(optionUser),
            p.GetValue(optionSkills), p.GetValue(optionAccess), ct));
        aichat.Subcommands.Add(send);

        cli.AddCommand(aichat);
    }

    async Task SendCommand(string message, string? page, Guid? userId, bool? skills, bool? access, CancellationToken ct)
    {
        using var scope = app.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiChatSessionStore>();
        var agent = scope.ServiceProvider.GetRequiredService<AiChatAgentService>();

        Guid user;
        if (userId is not null)
        {
            user = userId.Value;
        }
        else
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserService>();
            var admins = await users.ListDetail(new ListUserQuery { Roles = ["Admin"], Take = 1000 }, ct);
            var first = admins.Items.OrderBy(a => a.CreatedAt).FirstOrDefault();
            if (first is null)
            {
                OutResult(UserActionResult.Exception("no admin users found; pass --user explicitly", null));
                return;
            }
            user = first.Id;
            Console.WriteLine($"== default user: {first.UserName} ({first.Id})");
        }
        var state = new AiChatSessionState
        {
            Id = Guid.NewGuid(),
            UserId = user,
            Title = "cli",
            CreatedAtUtc = DateTime.UtcNow,
            ModifiedAtUtc = DateTime.UtcNow,
        };
        await store.SaveAsync(state, ct);

        Console.WriteLine($"== chat {state.Id} (user {user}), page: {page ?? "-"}, skills: {skills?.ToString().ToLower() ?? "default"}, access: {access?.ToString().ToLower() ?? "default"}");

        try
        {
            await agent.RunChatAsync(state.Id, user, message, page, ct, skills, access);
        }
        catch (Exception ex)
        {
            OutResult(UserActionResult.Exception(ex));
            return;
        }

        var after = await store.GetAsync(state.Id, user, ct);
        if (after is null)
        {
            OutResult(UserActionResult.Exception("session not found after run", null));
            return;
        }

        foreach (var m in after.Messages)
        {
            switch (m.Role)
            {
                case AiChatMessageRole.User:
                    break;
                case AiChatMessageRole.Assistant:
                    Console.WriteLine("\n[assistant]\n" + m.Content);
                    break;
                case AiChatMessageRole.Tool:
                    Console.WriteLine(m.IsToolResult
                        ? $"  <- {m.ToolName}: {Trunc(m.Content)}"
                        : $"  -> {m.ToolName} {Trunc(m.Content)}");
                    break;
                default:
                    Console.WriteLine($"[{m.Role}] {m.Content}");
                    break;
            }
        }

        OutResult(UserActionResult.Success());
    }

    static string Trunc(string? text, int max = 600)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= max ? text : text[..max] + "…";
    }
}
