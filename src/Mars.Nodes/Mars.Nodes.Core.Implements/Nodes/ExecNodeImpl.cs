using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
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

        var processInfo = new ProcessStartInfo
        {
            FileName = Node.Command,
            //Arguments = "-Command \"Get-ChildItem\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        var cmd = Process.Start(processInfo);

        if (Node.Append && input.Payload is not null)
        {
            cmd.StandardInput.WriteLine(input.Payload.ToString());
        }
        cmd.StandardInput.Flush();
        cmd.StandardInput.Close();
        cmd.WaitForExit();

        var output = cmd.StandardOutput.ReadToEnd();
        var cleanOutput = RemoveAnsiEscapeCodes(output);
        input.Payload = cleanOutput;

        callback(input);

        return Task.CompletedTask;
    }

    public static string RemoveAnsiEscapeCodes(string text)
    {
        // Регулярное выражение для удаления ANSI escape-кодов
        var ansiPattern = @"\x1B\[[0-9;]*[a-zA-Z]|\x1B\].*?\x07|\[[\d;]*m";
        return Regex.Replace(text, ansiPattern, string.Empty);
    }
}
