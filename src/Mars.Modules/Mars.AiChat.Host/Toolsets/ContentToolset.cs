using Mars.AiChat.Host.Tools;
using Mars.Nodes.Abstractions.Hubs;
using Mars.Cms.Abstractions.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;

namespace Mars.AiChat.Host.Toolsets;

/// <summary>
/// Посты: создание/чтение/список (per-run экземпляр с userId владельца чата).
/// </summary>
public class ContentToolset : IAiToolset
{
    private readonly IPostService _postService;
    private readonly IHubContext<ChatHub> _chatHub;

    public ContentToolset(IPostService postService, IHubContext<ChatHub> chatHub)
    {
        _postService = postService;
        _chatHub = chatHub;
    }

    public string Name => "content";

    public IReadOnlyList<AIFunction> Build(AiToolsetContext ctx)
    {
        var postTools = new MarsPostTools(_postService, _chatHub, ctx.UserId);
        return
        [
            AIFunctionFactory.Create(postTools.CreatePost),
            AIFunctionFactory.Create(postTools.GetPost),
            AIFunctionFactory.Create(postTools.ListPosts),
        ];
    }
}
