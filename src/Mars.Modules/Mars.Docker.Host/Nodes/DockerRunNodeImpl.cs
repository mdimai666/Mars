using Mars.Docker.Abstractions.Dto;
using Mars.Docker.Contracts.Nodes;
using Mars.Docker.Host.Services;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Contracts.Hubs;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Docker.Host.Nodes;

public class DockerRunNodeImpl : INodeImplement<DockerRunNode>
{
    public DockerRunNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public DockerRunNodeImpl(DockerRunNode node, IRuntimeNodeScope rns)
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
        var query = new RunContainerQuery
        {
            Image = Node.Image.Trim(),
            Cmd = DockerNodeHelper.SplitCommand(Node.Command),
            Env = DockerNodeHelper.SplitEnv(Node.Env),
            Stdin = string.IsNullOrEmpty(Node.Stdin) ? input.Payload?.ToString() : Node.Stdin,
            TimeoutSeconds = Node.TimeoutSeconds,
            KeepContainer = Node.KeepContainer,
        };

        RNS.Status(new NodeStatus("running…"));
        var result = await service.RunContainerOnce(query, parameters.CancellationToken);

        input.Payload = result;
        RNS.Status(new NodeStatus(result.TimedOut ? "timed out" : $"exit {result.ExitCode}"));
        callback(input);
    }
}
