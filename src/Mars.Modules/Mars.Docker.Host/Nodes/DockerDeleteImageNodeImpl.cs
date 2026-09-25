using Mars.Docker.Contracts.Nodes;
using Mars.Docker.Host.Services;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Contracts.Hubs;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Docker.Host.Nodes;

[NodeOutputValueSpec(typeof(string), Description = "deleted image")]
public class DockerDeleteImageNodeImpl : INodeImplement<DockerDeleteImageNode>
{
    public DockerDeleteImageNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public DockerDeleteImageNodeImpl(DockerDeleteImageNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var resolvedImage = ResolveImage();

        if (string.IsNullOrWhiteSpace(resolvedImage))
        {
            throw new NodeExecuteException(Node, "image is not configured");
        }

        var service = RNS.ServiceProvider.GetRequiredService<IDockerService>();
        var image = resolvedImage.Trim();

        await service.DeleteImage(image, parameters.CancellationToken);

        input.Payload = image;
        RNS.Status(new NodeStatus("deleted"));
        callback(input);

        string ResolveImage()
        {
            using var expr = RNS.Expressions(Node);
            return (string)expr.Resolve(Node.ImageKind, Node.Image, "string", input, Node, "Image")!;
        }
    }
}
