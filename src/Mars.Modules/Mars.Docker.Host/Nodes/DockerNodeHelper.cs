using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;

namespace Mars.Docker.Host.Nodes;

internal static class DockerNodeHelper
{
    public static string[] SplitCommand(string command)
        => command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string[] SplitEnv(string env)
        => env.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static async Task<string> ResolveContainerId(Services.IDockerService service, Node node, string container, CancellationToken cancellationToken)
    {
        var value = container.Trim();
        if (value.Length == 0)
        {
            throw new NodeExecuteException(node, "container is not configured");
        }

        if (value.Length == 64 && value.All(Uri.IsHexDigit))
        {
            return value;
        }

        var found = await service.GetContainerByName(value, cancellationToken)
            ?? throw new NodeExecuteException(node, $"container '{value}' not found");
        return found.ID;
    }
}
