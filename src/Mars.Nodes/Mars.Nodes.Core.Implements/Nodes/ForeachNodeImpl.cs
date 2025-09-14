using System.Collections;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class ForeachNodeImpl : INodeImplement<ForeachNode>, INodeImplement
{
    public ForeachNodeImpl(ForeachNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public ForeachNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (Node.Kind == EForeachKind.PayloadArray)
        {

            if (input.Payload is IEnumerable enumerable)
            {
                var arr = enumerable.Cast<object>();

                ForeachNode.ForeachCycle foreachCycle = new ForeachNode.ForeachCycle()
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
            ForeachNode.ForeachCycle foreachCycle = new ForeachNode.ForeachCycle()
            {
                index = 0,
                arr = Enumerable.Range(0, count).Cast<object>(),
                count = count,
            };
            input.Add(foreachCycle);
        }
        else throw new NotImplementedException();

        callback(input);

        return Task.CompletedTask;
    }
}

public class ForeachIterateNodeImpl : INodeImplement<ForeachIterateNode>, INodeImplement
{
    public ForeachIterateNodeImpl(ForeachIterateNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public ForeachIterateNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        ForeachNode.ForeachCycle? cycle = input.Get<ForeachNode.ForeachCycle>();

        if (cycle is null)
        {
            throw new ArgumentException("cycle not found in Msg");
        }

        if (cycle.index < cycle.count)
        {
            NodeMsg _input = new NodeMsg();
            _input.Payload = cycle.arr.ElementAt(cycle.index);
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
