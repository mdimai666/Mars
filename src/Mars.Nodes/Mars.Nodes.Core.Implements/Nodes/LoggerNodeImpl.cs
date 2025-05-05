using System.Text.Json;
using Mars.Nodes.Core.Nodes;
using Mars.Core.Extensions;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Core.Implements.Nodes;

public class LoggerNodeImpl : INodeImplement<LoggerNode>, INodeImplement
{

    public LoggerNode Node { get; }
    Node INodeImplement<Node>.Node => Node;
    public IRED RED { get; set; }

    public LoggerNodeImpl(LoggerNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {
        try
        {
            int jsonSymbolsLimit = 10_000;

            var opt = new JsonSerializerOptions
            {
                IncludeFields = false,
                MaxDepth = 0,
                WriteIndented = true,
                //Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            };


            string loggerContent;

            if (input.Payload is not string && input.Payload is object)
            {
                //string json = Newtonsoft.Json.JsonConvert.SerializeObject(input.Payload, Newtonsoft.Json.Formatting.Indented);
                string json = System.Text.Json.JsonSerializer.Serialize(input.Payload, opt);

                loggerContent = json.Left(jsonSymbolsLimit);
            }
            else
            {
                loggerContent = input.Payload?.ToString()?.TextEllipsis(500) ?? "null";
            }

            RED.Logger.Log((Microsoft.Extensions.Logging.LogLevel)Node.Level, loggerContent);

            RED.Status(new NodeStatus(DateTime.Now.ToString("HH:mm:ss.fff")));
        }
        catch (Exception ex)
        {
            RED.Logger.LogError(ex, "LoggerNode:ERROR " + ex.Message);
        }

        return Task.CompletedTask;
    }
}
