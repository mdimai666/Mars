using System.Text.Json;
using Mars.Core.Extensions;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Core.Implements.Nodes;

public class LoggerNodeImpl : INodeImplement<LoggerNode>
{
    public LoggerNode Node { get; }
    Node INodeImplement.Node => Node;
    public IRuntimeNodeScope RNS { get; set; }
    private readonly ILogger<IRuntimeNodeScope> _logger;

    public LoggerNodeImpl(LoggerNode node, IRuntimeNodeScope rns, ILogger<IRuntimeNodeScope> logger)
    {
        Node = node;
        RNS = rns;
        _logger = logger;
    }

    static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        IncludeFields = false,
        MaxDepth = 0,
        WriteIndented = true,
        //Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
    };

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        try
        {
            int jsonSymbolsLimit = 10_000;

            string loggerContent;

            if (input.Payload is not string && input.Payload is object)
            {
                string json = System.Text.Json.JsonSerializer.Serialize(input.Payload, _jsonSerializerOptions);

                loggerContent = json.Left(jsonSymbolsLimit);
            }
            else
            {
                loggerContent = input.Payload?.ToString()?.TextEllipsis(500) ?? "null";
            }

            _logger.Log((Microsoft.Extensions.Logging.LogLevel)Node.Level, loggerContent);

            RNS.Status(new NodeStatus(DateTime.Now.ToString("HH:mm:ss.fff")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoggerNode:ERROR " + ex.Message);
        }

        return Task.CompletedTask;
    }
}
