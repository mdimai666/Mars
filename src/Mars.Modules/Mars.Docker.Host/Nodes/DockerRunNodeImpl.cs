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
        var (image, command, env, stdin) = ResolveFields();

        if (string.IsNullOrWhiteSpace(image))
        {
            throw new NodeExecuteException(Node, "image is not configured");
        }

        var service = RNS.ServiceProvider.GetRequiredService<IDockerService>();
        var query = new RunContainerQuery
        {
            Image = image.Trim(),
            Cmd = DockerNodeHelper.SplitCommand(command),
            Env = DockerNodeHelper.SplitEnv(env),
            Stdin = string.IsNullOrEmpty(stdin) ? input.Payload?.ToString() : stdin,
            TimeoutSeconds = Node.TimeoutSeconds,
            KeepContainer = Node.KeepContainer,
        };

        RNS.Status(new NodeStatus("running…"));
        var result = await service.RunContainerOnce(query, parameters.CancellationToken);

        input.Payload = result;
        RNS.Status(new NodeStatus(result.TimedOut ? "timed out" : $"exit {result.ExitCode}"));
        callback(input);

        // прогон контейнера — долгий I/O: аренда runner'а сужена до резолвинга полей
        (string Image, string Command, string Env, string Stdin) ResolveFields()
        {
            using var expr = RNS.Expressions(Node);
            return (
                (string)expr.Resolve(Node.ImageKind, Node.Image, "string", input, Node, "Image")!,
                (string)expr.Resolve(Node.CommandKind, Node.Command, "string", input, Node, "Command")!,
                (string)expr.Resolve(Node.EnvKind, Node.Env, "string", input, Node, "Env")!,
                (string)expr.Resolve(Node.StdinKind, Node.Stdin, "string", input, Node, "Stdin")!);
        }
    }
}
