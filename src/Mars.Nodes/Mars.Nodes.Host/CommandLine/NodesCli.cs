using System.CommandLine;
using Mars.Core.Exceptions;
using Mars.Host.Shared.CommandLine;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Utils;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared.Services;
using Mars.Shared.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host.CommandLine;

public class NodesCli : CommandCli
{
    public NodesCli(ICommandLineApi cli) : base(cli)
    {
        var nodeCommand = new Command("node", "Nodes subcommand");

        var argumenNodeIdOrName = new Argument<string>("IdOrName");
        var optionNameFilter = new Option<string>("--nameFilter", "-f") { Description = "name filter" };

        //list
        var nodeListCommand = new Command("list", "list nodes") { optionNameFilter };
        nodeListCommand.SetAction(p => NodeListCommand(p.GetValue(optionNameFilter)));
        nodeCommand.Subcommands.Add(nodeListCommand);

        //inject
        var nodeInjectCommand = new Command("inject", "inject node by Id or Name") { argumenNodeIdOrName };
        nodeInjectCommand.SetAction((p, ct) => NodeInjectCommand(p.GetRequiredValue(argumenNodeIdOrName), ct));
        nodeCommand.Subcommands.Add(nodeInjectCommand);

        cli.AddCommand(nodeCommand);
    }

    public void NodeListCommand(string? filter)
    {
        //here the service has not yet load flows.json file and has not started
        var nodeService = app.Services.GetRequiredService<INodeService>();

        if (nodeService.TryReadFlowFile(out var flowFile))
        {

            var nodesList = flowFile.Nodes.Where(node => node is InjectNode)
                                    .Where(node => filter == null || node.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                                    .Select(node => (string[])[node.GetType().Name, node.Id, node.Name]);

            var table = new ConsoleTable([
                ["Type", "NodeId", "Name"],
            ..nodesList
            ]);
            Console.WriteLine(table);
        }
        else
        {
            Console.Error.WriteLine("Cannot load FlowFile");
        }
    }

    public async Task NodeInjectCommand(string nodeIdOrName, CancellationToken cancellationToken)
    {
        var nodeTaskManager = app.Services.GetRequiredService<INodeTaskManager>();
        var nodeService = app.Services.GetRequiredService<INodeService>();
        nodeService.Setup();
        using var scope = app.Services.CreateScope();
        var nodes = nodeService.BaseNodes;
        var node = nodeService.BaseNodes.GetValueOrDefault(nodeIdOrName)
                    ?? nodeService.BaseNodes.Values.SingleOrDefault(node => node is InjectNode && node.Name.Length > 0 && node.Name == nodeIdOrName);

        try
        {
            if (node is null) throw new NotFoundException($"Node with Id or Name '{nodeIdOrName}' not found");

            var taskId = await nodeTaskManager.CreateJob(scope.ServiceProvider, node.Id, new());
            var task = nodeTaskManager.GetDetail(taskId);
            var status = task.IsDone ? "IsDone" : (task.IsTerminated ? "IsTerminated" : "-none-");
            var delay = (task.EndDate!.Value - task.StartDate).TotalMilliseconds.ToString("0ms");
            var table = new ConsoleTable([
                ["Name", "Message"],
                ["Status", status],
                ["ExecuteCount", task.ExecuteCount.ToString()],
                ["Duration", delay],
                ["ErrorCount", task.ErrorCount.ToString()],
            ]);
            Console.WriteLine(table);

            //output errors
            foreach (var job in task.Jobs)
            {
                foreach (var execution in job.Executions.Where(s => s.Exception != null))
                {
                    Console.WriteLine($"{nodes[job.NodeId].Type} [{job.NodeId}]");
                    Console.WriteLine(execution.Exception?.Message);
                }
            }
        }
        catch (Exception ex)
        {
            OutResult(UserActionResult.Exception(ex));
        }
    }
}
