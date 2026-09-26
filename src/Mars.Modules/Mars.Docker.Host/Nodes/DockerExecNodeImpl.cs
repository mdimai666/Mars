using Mars.Docker.Abstractions.Dto;
using Mars.Docker.Contracts;
using Mars.Docker.Contracts.Nodes;
using Mars.Docker.Host.Services;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Contracts.Hubs;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Docker.Host.Nodes;

[NodeOutputValueSpec(typeof(DockerRunResultResponse))]
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
        var (containerName, command, env, workingDir, user) = ResolveFields();

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new NodeExecuteException(Node, "command is not configured");
        }

        var service = RNS.ServiceProvider.GetRequiredService<IDockerService>();
        var containerId = await DockerNodeHelper.ResolveContainerId(service, Node, containerName, parameters.CancellationToken);

        var query = new ExecContainerQuery
        {
            Cmd = DockerNodeHelper.SplitCommand(command),
            Env = DockerNodeHelper.SplitEnv(env),
            WorkingDir = string.IsNullOrWhiteSpace(workingDir) ? null : workingDir.Trim(),
            User = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
            Stdin = input.Payload?.ToString(),
            TimeoutSeconds = Node.TimeoutSeconds,
        };

        var result = await service.ExecInContainer(containerId, query, parameters.CancellationToken);

        input.Payload = result;
        RNS.Status(new NodeStatus(result.TimedOut ? "timed out" : $"exit {result.ExitCode}"));
        callback(input);

        // docker exec — долгий I/O: аренда runner'а сужена до резолвинга полей
        (string ContainerName, string Command, string Env, string WorkingDir, string User) ResolveFields()
        {
            using var expr = RNS.Expressions(Node);
            return (
                (string)expr.Resolve(Node.ContainerNameKind, Node.ContainerName, "string", input, Node, "ContainerName")!,
                (string)expr.Resolve(Node.CommandKind, Node.Command, "string", input, Node, "Command")!,
                (string)expr.Resolve(Node.EnvKind, Node.Env, "string", input, Node, "Env")!,
                (string)expr.Resolve(Node.WorkingDirKind, Node.WorkingDir, "string", input, Node, "WorkingDir")!,
                (string)expr.Resolve(Node.UserKind, Node.User, "string", input, Node, "User")!);
        }
    }
}
