using Mars.Docker.Contracts.Nodes;
using Mars.Docker.Host.Services;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Docker.Host.Nodes;

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
        if (string.IsNullOrWhiteSpace(Node.Image))
        {
            throw new NodeExecuteException(Node, "image is not configured");
        }

        var service = RNS.ServiceProvider.GetRequiredService<IDockerService>();
        var image = Node.Image.Trim();
        var tag = string.IsNullOrWhiteSpace(Node.Tag) ? "latest" : Node.Tag.Trim();

        RNS.Status(new NodeStatus($"pulling {image}:{tag}…"));
        var progress = new Progress<string>(text => RNS.Status(new NodeStatus(text)));
        await service.PullImage(image, tag, progress, parameters.CancellationToken);

        input.Payload = $"{image}:{tag}";
        RNS.Status(new NodeStatus("pulled"));
        callback(input);
    }
}
