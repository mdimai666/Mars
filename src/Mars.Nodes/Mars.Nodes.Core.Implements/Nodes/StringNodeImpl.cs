using System.Collections;
using System.Text;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class StringNodeImpl : INodeImplement<StringNode>, INodeImplement
{
    public StringNodeImpl(StringNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public StringNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback)
    {
        if (input.Payload is null)
        {
            callback(input);
            return Task.CompletedTask;
        }

        string payload;

        /*if (input.Payload is IEnumerable enumerable)
        {
            payload = enumerable.Cast<string>().ToArray();
        }
        else */if (input.Payload is not string && !input.GetType().IsPrimitive)
        {
            throw new ArgumentException("input must be string or primitive");
        }
        else
        {
            //payload = [input.Payload.ToString()!];
            payload = input.Payload is string st ? st : input.Payload.ToString()!;
        }

        foreach (var op in Node.Operations)
        {
            var result = Operate(Enum.Parse<BaseOperation>(op.Name), payload, op.Args);
            payload = result;
        }

        input.Payload = payload;
        //input.Payload = Encode(input.Payload, ;

        //Encoding.GetEncodings();

        callback(input);

        return Task.CompletedTask;
    }

    public static string Encode(string input, string sourceEncode, string destEncode)
    {
        Encoding source = Encoding.GetEncoding(sourceEncode);
        Encoding dest = Encoding.GetEncoding(destEncode);

        return dest.GetString(source.GetBytes(input));
    }

    public static string[] GetEncodings()
    {
        return Encoding.GetEncodings().Select(s => $"{s.DisplayName} ({s.CodePage})").ToArray();
    }

    public string Operate(BaseOperation operation, string input, string[] args)
        => operation switch
        {
            BaseOperation.ToUpper => input.ToUpper(),
            BaseOperation.ToLower => input.ToLower(),
            BaseOperation.Trim => input.Trim(),
            BaseOperation.TrimStart => input.TrimStart(),
            BaseOperation.TrimEnd => input.TrimEnd(),
            BaseOperation.Replace => input.Replace(args[0], args[1]),
            //BaseOperation.Split => input.Split(args[0]),
            //BaseOperation.Join => input.Join(args[0]),
            //BaseOperation.Format => string.Format(args[0]),
            //BaseOperation.Encode => Encode(input, args[0], args[1]),
            _ => throw new NotImplementedException()
        };
}
