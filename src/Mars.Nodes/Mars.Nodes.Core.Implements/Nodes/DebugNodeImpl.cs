using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Core.Extensions;
using Mars.Nodes.Core.Implements.JsonConverters;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class DebugNodeImpl : INodeImplement<DebugNode>, INodeImplement
{

    public DebugNode Node { get; }
    Node INodeImplement<Node>.Node => Node;
    public IRED RED { get; set; }

    public DebugNodeImpl(DebugNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        IncludeFields = false,
        MaxDepth = 0,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new IgnoreReadOnlySpanPropertiesResolver(),
    };

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        try
        {
            int jsonSymbolsLimit = 1000;

            DebugMessage msg;

            if (Node.CompleteInputMessage)
            {
                string json = System.Text.Json.JsonSerializer.Serialize(input.AsFullDict(), _jsonSerializerOptions);

                msg = new DebugMessage
                {
                    NodeId = Node.Id,
                    Message = "DebugNode (complete):",
                    Json = json.TextEllipsis(jsonSymbolsLimit),
                    Level = Node.Level ?? Mars.Core.Models.MessageIntent.Info,
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
                    Level = Node.Level ?? Mars.Core.Models.MessageIntent.Info,
                };
            }
            else
            {
                msg = new DebugMessage
                {
                    NodeId = Node.Id,
                    Message = input.Payload?.ToString()?.TextEllipsis(500) ?? "null",
                    Level = Node.Level ?? Mars.Core.Models.MessageIntent.Info,
                };
            }

            RED.DebugMsg(msg);

#if DEBUG
            //RED.logger.LogWarning("DebugNode", msg);
#endif

            //RED.DebugMsg(new DebugMessage { message = "dealayed" });
            //Random r = new Random();
            //RED.Status(new NodeStatus { Text = "Yee " + r.Next(1, 99) });

            if (Node.ShowPayloadTypeInStatus)
            {
                RED.Status(new NodeStatus(input.Payload?.GetType().Name + " | " + DateTime.Now.ToString("HH:mm:ss.fff")));
            }
            else
            {
                RED.Status(new NodeStatus(DateTime.Now.ToString("HH:mm:ss.fff")));
            }
        }
        catch (Exception ex)
        {
            RED.DebugMsg(new DebugMessage
            {
                NodeId = Node.Id,
                Message = "DebugNode:ERROR = " + ex.Message,
                Level = Mars.Core.Models.MessageIntent.Error,
            });
#if DEBUG
            //RED.logger.LogError(ex, "DebugNode:ERROR" + ex.Message);
#endif
        }

        return Task.CompletedTask;
    }
}
