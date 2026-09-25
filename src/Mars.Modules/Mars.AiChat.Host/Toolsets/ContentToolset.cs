using Mars.AiChat.Host.Tools;
using Mars.Cms.Abstractions.Services;
using Mars.Nodes.Abstractions.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;

namespace Mars.AiChat.Host.Toolsets;

/// <summary>
/// Посты: описание типа, создание/чтение/список/обновление (per-run экземпляр с userId владельца чата).
/// </summary>
public class ContentToolset : IAiToolset
{
    private readonly IPostService _postService;
    private readonly IPostJsonService _postJsonService;
    private readonly IMetaModelTypesLocator _typesLocator;
    private readonly IHubContext<ChatHub> _chatHub;

    public ContentToolset(IPostService postService, IPostJsonService postJsonService,
                          IMetaModelTypesLocator typesLocator, IHubContext<ChatHub> chatHub)
    {
        _postService = postService;
        _postJsonService = postJsonService;
        _typesLocator = typesLocator;
        _chatHub = chatHub;
    }

    public string Name => "content";

    public IReadOnlyList<AIFunction> Build(AiToolsetContext ctx)
    {
        var postTools = new MarsPostTools(_postService, _postJsonService, _typesLocator, _chatHub, ctx.UserId);
        return
        [
            AIFunctionFactory.Create(postTools.DescribePostType),
            AIFunctionFactory.Create(postTools.CreatePost),
            AIFunctionFactory.Create(postTools.GetPost),
            AIFunctionFactory.Create(postTools.ListPosts),
            AIFunctionFactory.Create(postTools.UpdatePost),
        ];
    }
}
