using Mars.Docker.Abstractions.Dto;
using Mars.Docker.Contracts.Nodes;
using Mars.Docker.Host.Services;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Docker.Host.Nodes;

public class DockerExecNodeImpl : INodeImplement<DockerExecNode>
{
    public DockerExecNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public DockerExecNodeImpl(DockerExecNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(Node.Command))
        {
            throw new NodeExecuteException(Node, "command is not configured");
        }

        var service = RNS.ServiceProvider.GetRequiredService<IDockerService>();
        var containerId = await DockerNodeHelper.ResolveContainerId(service, Node, Node.ContainerName, parameters.CancellationToken);

        var query = new ExecContainerQuery
        {
            Cmd = DockerNodeHelper.SplitCommand(Node.Command),
            Env = DockerNodeHelper.SplitEnv(Node.Env),
            WorkingDir = string.IsNullOrWhiteSpace(Node.WorkingDir) ? null : Node.WorkingDir.Trim(),
            User = string.IsNullOrWhiteSpace(Node.User) ? null : Node.User.Trim(),
            Stdin = input.Payload?.ToString(),
            TimeoutSeconds = Node.TimeoutSeconds,
        };

        var result = await service.ExecInContainer(containerId, query, parameters.CancellationToken);

        input.Payload = result;
        RNS.Status(new NodeStatus(result.TimedOut ? "timed out" : $"exit {result.ExitCode}"));
        callback(input);
    }
}
