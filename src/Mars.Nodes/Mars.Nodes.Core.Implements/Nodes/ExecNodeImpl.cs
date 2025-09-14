using System.Diagnostics;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class ExecNodeImpl : INodeImplement<ExecNode>, INodeImplement
{

    public ExecNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public ExecNodeImpl(ExecNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        Process cmd = new Process();
        cmd.StartInfo.FileName = Node.Command;

        cmd.StartInfo.RedirectStandardInput = true;
        cmd.StartInfo.RedirectStandardOutput = true;
        cmd.StartInfo.CreateNoWindow = false;
        cmd.StartInfo.UseShellExecute = false;
        cmd.Start();

        if (Node.Append && input.Payload is not null)
        {
            cmd.StandardInput.WriteLine(input.Payload.ToString());
        }
        cmd.StandardInput.Flush();
        cmd.StandardInput.Close();
        cmd.WaitForExit();

        input.Payload = cmd.StandardOutput.ReadToEnd();

        callback(input);

        return Task.CompletedTask;
    }
}
