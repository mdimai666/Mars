using Mars.Docker.Contracts.Nodes;
using Mars.Docker.Host.Services;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Contracts.Hubs;
using Mars.Nodes.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Docker.Host.Nodes;

public class DockerStateNodeImpl : INodeImplement<DockerStateNode>
{
    public DockerStateNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public DockerStateNodeImpl(DockerStateNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var service = RNS.ServiceProvider.GetRequiredService<IDockerService>();
        var ct = parameters.CancellationToken;
        var containerId = await DockerNodeHelper.ResolveContainerId(service, Node, Node.ContainerName, ct);

        switch (Node.Action)
        {
            case DockerStateAction.Start:
                await service.StartContainer(containerId, ct);
                break;
            case DockerStateAction.Stop:
                await service.StopContainer(containerId, ct);
                break;
            case DockerStateAction.Restart:
                await service.RestartContainer(containerId, ct);
                break;
            case DockerStateAction.Pause:
                await service.PauseContainer(containerId, ct);
                break;
            case DockerStateAction.Unpause:
                await service.UnpauseContainer(containerId, ct);
                break;
            case DockerStateAction.Delete:
                await service.DeleteContainer(containerId, ct);
                input.Payload = containerId;
                RNS.Status(new NodeStatus("deleted"));
                callback(input);
                return;
        }

        var state = (await service.GetContainer(containerId, ct))?.State;
        input.Payload = state;
        RNS.Status(new NodeStatus($"{Node.Action}: {state}"));
        callback(input);
    }
}
