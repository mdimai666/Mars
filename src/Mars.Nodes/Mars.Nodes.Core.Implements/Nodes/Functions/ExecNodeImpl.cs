using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Functions;

public class ExecNodeImpl : INodeImplement<ExecNode>
{
    public ExecNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public ExecNodeImpl(ExecNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        var processInfo = new ProcessStartInfo
        {
            FileName = Node.Command,
            //Arguments = "-Command \"Get-ChildItem\"",
            Arguments = Node.Append ? input.Payload.ToString() : string.Empty,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        var cmd = Process.Start(processInfo);

        //if (Node.Append && input.Payload is not null)
        //{
        //    cmd.StandardInput.WriteLine(input.Payload.ToString());
        //}
        cmd.StandardInput.Flush();
        cmd.StandardInput.Close();
        await cmd.WaitForExitAsync(parameters.CancellationToken);

        var output = await cmd.StandardOutput.ReadToEndAsync();
        var error = await cmd.StandardError.ReadToEndAsync();

        var cleanOutput = RemoveAnsiEscapeCodes(output);
        input.Payload = cleanOutput;

        callback(input);
    }

    public static string RemoveAnsiEscapeCodes(string text)
    {
        // Регулярное выражение для удаления ANSI escape-кодов
        var ansiPattern = @"\x1B\[[0-9;]*[a-zA-Z]|\x1B\].*?\x07|\[[\d;]*m";
        return Regex.Replace(text, ansiPattern, string.Empty);
    }
}
