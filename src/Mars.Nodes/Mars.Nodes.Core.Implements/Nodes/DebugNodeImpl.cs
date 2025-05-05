using System.Text.Json;
using Mars.Nodes.Core.Nodes;
using Mars.Core.Extensions;

namespace Mars.Nodes.Core.Implements.Nodes;

public class DebugNodeImpl : INodeImplement<DebugNode>, INodeImplement
{

    public DebugNode Node { get; }
    Node INodeImplement<Node>.Node => Node;
    public IRED RED { get; set; }

    public DebugNodeImpl(DebugNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {
        try
        {
            int jsonSymbolsLimit = 1000;

            var opt = new JsonSerializerOptions
            {
                IncludeFields = false,
                MaxDepth = 0,
                WriteIndented = true,
                //Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            };


            DebugMessage msg;

            if (Node.CompleteInputMessage)
            {
                //string json = Newtonsoft.Json.JsonConvert.SerializeObject(input.AsFullDict(), Newtonsoft.Json.Formatting.Indented, stt);
                string json = System.Text.Json.JsonSerializer.Serialize(input.AsFullDict(), opt);

                msg = new DebugMessage
                {
                    id = Node.Id,
                    message = "DebugNode:",
                    json = json.Left(jsonSymbolsLimit),
                    Level = Node.Level ?? Mars.Core.Models.MessageIntent.Info,
                };
            }
            else if (input.Payload is not string && input.Payload is object)
            {

                //string json = Newtonsoft.Json.JsonConvert.SerializeObject(input.Payload, Newtonsoft.Json.Formatting.Indented);
                string json = System.Text.Json.JsonSerializer.Serialize(input.Payload, opt);

                msg = new DebugMessage
                {
                    id = Node.Id,
                    message = "DebugNode:",
                    json = json.Left(jsonSymbolsLimit),
                    Level = Node.Level ?? Mars.Core.Models.MessageIntent.Info,
                };
            }
            else
            {
                msg = new DebugMessage
                {
                    id = Node.Id,
                    message = input.Payload?.ToString()?.TextEllipsis(500) ?? "null",
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
                id = Node.Id,
                message = "DebugNode:ERROR = " + ex.Message,
                Level = Mars.Core.Models.MessageIntent.Error,
            });
#if DEBUG
            //RED.logger.LogError(ex, "DebugNode:ERROR" + ex.Message);
#endif
        }

        return Task.CompletedTask;
    }
}
