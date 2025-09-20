using System.Collections;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class ForeachNodeImpl : INodeImplement<ForeachNode>, INodeImplement
{
    public ForeachNodeImpl(ForeachNode node, IRED RED)
    {
        Node = node;
        this.RED = RED;
    }

    public ForeachNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

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
                    arr = arr,
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
                arr = Enumerable.Range(0, count).Cast<object>(),
                count = count,
            };
            input.Add(foreachCycle);
        }
        else throw new NotImplementedException();

        //callback(input, 1);

        //return Task.CompletedTask;

        return IterateStep(input, callback, parameters);
    }

    private Task IterateStep(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        ForeachNode.ForeachCycle? cycle = input.Get<ForeachNode.ForeachCycle>();

        if (cycle is null)
        {
            throw new ArgumentException("cycle not found in Msg");
        }

        if (cycle.index < cycle.count)
        {
            NodeMsg _input = new()
            {
                Payload = cycle.arr.ElementAt(cycle.index)
            };
            //input.Payload = cycle.arr.ElementAt(cycle.index);
            cycle.index++;
            _input.Add(cycle);
            RED.Status(new NodeStatus($"{cycle.index}/{cycle.count}"));
            callback(_input, 1);
        }
        else
        {
            RED.Status(new NodeStatus($"{cycle.index}/{cycle.count} complete"));

            NodeMsg _input = new() { Payload = cycle.count };
            callback(_input, 0);
        }

        return Task.CompletedTask;
    }
}
