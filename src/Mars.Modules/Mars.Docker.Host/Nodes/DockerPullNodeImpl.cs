using Mars.Docker.Contracts.Nodes;
using Mars.Docker.Host.Services;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Contracts.Hubs;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Docker.Host.Nodes;

[NodeOutputValueSpec(typeof(string), Description = "pulled image:tag")]
public class DockerPullNodeImpl : INodeImplement<DockerPullNode>
{
    public DockerPullNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public DockerPullNodeImpl(DockerPullNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var (resolvedImage, resolvedTag) = ResolveFields();

        if (string.IsNullOrWhiteSpace(resolvedImage))
        {
            throw new NodeExecuteException(Node, "image is not configured");
        }

        var service = RNS.ServiceProvider.GetRequiredService<IDockerService>();
        var image = resolvedImage.Trim();
        var tag = string.IsNullOrWhiteSpace(resolvedTag) ? "latest" : resolvedTag.Trim();

        RNS.Status(new NodeStatus($"pulling {image}:{tag}…"));
        var progress = new Progress<string>(text => RNS.Status(new NodeStatus(text)));
        await service.PullImage(image, tag, progress, parameters.CancellationToken);

        input.Payload = $"{image}:{tag}";
        RNS.Status(new NodeStatus("pulled"));
        callback(input);

        // pull образа — долгий I/O: аренда runner'а сужена до резолвинга полей
        (string Image, string Tag) ResolveFields()
        {
            using var expr = RNS.Expressions(Node);
            return (
                (string)expr.Resolve(Node.ImageKind, Node.Image, "string", input, Node, "Image")!,
                (string)expr.Resolve(Node.TagKind, Node.Tag, "string", input, Node, "Tag")!);
        }
    }
}
