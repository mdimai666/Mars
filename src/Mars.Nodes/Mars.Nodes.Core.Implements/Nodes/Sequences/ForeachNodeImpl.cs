using System.Collections;
using Mars.Nodes.Core.Nodes.Sequences;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Sequences;

public class ForeachNodeImpl : INodeImplement<ForeachNode>
{
    public ForeachNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public ForeachNodeImpl(ForeachNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        return parameters.InputPort switch
        {
            0 => CreateIterator(input, callback, parameters),
            1 => IterateStep(input, callback, parameters),
            _ => throw new NotImplementedException()
        };
    }

    private Task CreateIterator(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (Node.Kind == EForeachKind.PayloadArray)
        {

            if (input.Payload is IEnumerable enumerable)
            {
                var arr = enumerable.Cast<object>();

                ForeachNode.ForeachCycle foreachCycle = new()
                {
                    index = 0,
                    arr = arr.ToArray(),
                    count = arr.Count(),
                };
                input.Add(foreachCycle);

            }
            else
            {
                throw new ArgumentException("input is not IEnumerable<>");
            }
        }
        else if (Node.Kind == EForeachKind.Repeat)
        {
            int count;
            if (Node.RepeatCount is not null)
            {
                count = Node.RepeatCount.Value;
            }
            else
            {
                try
                {
                    count = (input.Payload is int _intVal) ? _intVal : int.Parse(input.Payload.ToString()!);
                }
                catch (FormatException ex)
                {
                    throw new FormatException("payload must be number", ex);
                }
            }
            ForeachNode.ForeachCycle foreachCycle = new()
            {
                index = 0,
                arr = [.. Enumerable.Range(0, count)],
                count = count,
            };
            input.Add(foreachCycle);
        }
        else throw new NotImplementedException();

        return IterateStep(input, callback, parameters);
    }

    private Task IterateStep(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        ForeachNode.ForeachCycle? cycle = input.Get<ForeachNode.ForeachCycle>();

        if (cycle is null)
        {
            throw new ArgumentException("cycle not found in Msg");
        }

        parameters.CancellationToken.ThrowIfCancellationRequested();

        if (cycle.index < cycle.count)
        {
            input.Payload = cycle.arr[cycle.index];
            cycle.index++;
            input.Set(cycle);
            RNS.Status(new NodeStatus($"{cycle.index}/{cycle.count}"));
            callback(input, 1);
        }
        else
        {
            RNS.Status(new NodeStatus($"{cycle.index}/{cycle.count} complete"));

            input.Payload = cycle.count;
            callback(input, 0);
        }

        return Task.CompletedTask;
    }
}
