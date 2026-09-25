using Mars.Docker.Contracts.Nodes;
using Mars.Docker.Host.Services;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Contracts.Hubs;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Docker.Host.Nodes;

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
        if (string.IsNullOrWhiteSpace(Node.Image))
        {
            throw new NodeExecuteException(Node, "image is not configured");
        }

        var service = RNS.ServiceProvider.GetRequiredService<IDockerService>();
        var image = Node.Image.Trim();

        await service.DeleteImage(image, parameters.CancellationToken);

        input.Payload = image;
        RNS.Status(new NodeStatus("deleted"));
        callback(input);
    }
}
