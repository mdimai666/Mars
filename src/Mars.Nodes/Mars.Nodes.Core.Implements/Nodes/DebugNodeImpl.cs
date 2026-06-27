using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Core.Extensions;
using Mars.Nodes.Core.Implements.JsonConverters;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes;

public class DebugNodeImpl : INodeImplement<DebugNode>
{

    public DebugNode Node { get; }
    Node INodeImplement.Node => Node;
    public IRuntimeNodeScope RNS { get; set; }

    public DebugNodeImpl(DebugNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        IncludeFields = false,
        MaxDepth = 0,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new IgnoreReadOnlySpanPropertiesResolver(),
        Converters = { new ExceptionConverterFactory() }
    };

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        try
        {
            int jsonSymbolsLimit = 20_000;
            var intentLevel = Node.Level ??
                               (input.Payload is Exception
                                    ? Mars.Core.Models.MessageIntent.Error
                                    : Mars.Core.Models.MessageIntent.Info);
            DebugMessage msg;

            if (Node.CompleteInputMessage)
            {
                string json = System.Text.Json.JsonSerializer.Serialize(input.AsFullDict(), _jsonSerializerOptions);

                msg = new DebugMessage
                {
                    NodeId = Node.Id,
                    Message = "DebugNode (complete):",
                    Json = json.TextEllipsis(jsonSymbolsLimit),
                    Level = intentLevel,
                };
            }
            else if (input.Payload is not string && input.Payload is object)
            {
                string json = System.Text.Json.JsonSerializer.Serialize(input.Payload, _jsonSerializerOptions);

                msg = new DebugMessage
                {
                    NodeId = Node.Id,
                    Message = $"DebugNode (serialized)\nType=({input.Payload?.GetType().Name}):",
                    Json = json.TextEllipsis(jsonSymbolsLimit),
                    Level = intentLevel,
                };
            }
            else
            {
                msg = new DebugMessage
                {
                    NodeId = Node.Id,
                    Message = input.Payload?.ToString()?.TextEllipsis(500) ?? "null",
                    Level = intentLevel,
                };
            }

            RNS.DebugMsg(msg);

#if DEBUG
            //RNS.logger.LogWarning("DebugNode", msg);
#endif

            //RNS.DebugMsg(new DebugMessage { message = "dealayed" });
            //Random r = new Random();
            //RNS.Status(new NodeStatus { Text = "Yee " + r.Next(1, 99) });

            if (Node.ShowPayloadTypeInStatus)
            {
                RNS.Status(new NodeStatus(input.Payload?.GetType().Name + " | " + DateTime.Now.ToString("HH:mm:ss.fff")));
            }
            else
            {
                RNS.Status(new NodeStatus(DateTime.Now.ToString("HH:mm:ss.fff")));
            }

            if (Node.WriteToConsole)
            {
                var outputBody = msg.Json?.IsNullOrEmpty() == true ? "" : ("\n" + msg.Json);
                Console.WriteLine(msg.Message + outputBody);
            }
        }
        catch (Exception ex)
        {
            RNS.DebugMsg(new DebugMessage
            {
                NodeId = Node.Id,
                Message = "DebugNode:ERROR = " + ex.Message,
                Level = Mars.Core.Models.MessageIntent.Error,
            });
#if DEBUG
            //RNS.logger.LogError(ex, "DebugNode:ERROR" + ex.Message);
#endif
        }

        return Task.CompletedTask;
    }
}
