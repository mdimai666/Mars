using Mars.Core.Features.JsonConverter;
using Mars.Nodes.Core.Implements.Models;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;
using Newtonsoft.Json;

namespace Mars.Nodes.Core.Implements.Nodes;

public class JsonNodeImpl : INodeImplement<JsonNode>
{
    public JsonNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public JsonNodeImpl(JsonNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (input.Payload is null)
        {
            callback(input);
            return Task.CompletedTask;
        }

        object payload = input.Payload;

        if (Node.Action == JsonNode.JsonNodeAction.ToJsonString)
        {
            string json = JsonNodeImpl.ToJsonString(payload, true);
            input.Payload = json;
        }
        else if (Node.Action == JsonNode.JsonNodeAction.ToObject)
        {
            var v = ParseString(payload.ToString()!);
            input.Payload = new DynamicJson(v);
        }
        else
        {
            if (payload is string jsonPayload)
            {
                var v = ParseString(jsonPayload);
                input.Payload = new DynamicJson(v);
            }
            else
            {
                string json = JsonNodeImpl.ToJsonString(payload, true);
                input.Payload = json;
            }
        }

        callback(input);

        return Task.CompletedTask;
    }

    static JsonSerializerSettings _defaultConvertSetting = new()
    {
        Formatting = Formatting.None,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    static JsonSerializerSettings _defaultConvertSettingNotFormatted = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Formatting = Formatting.None
    };

    public static string ToJsonString(object input, bool formatted = false)
    {
        if (input is Newtonsoft.Json.Linq.JToken token)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(input,
                formatted ? _defaultConvertSetting : _defaultConvertSettingNotFormatted);
        }
        else if (input is System.Text.Json.Nodes.JsonNode jsonNode)
        {
            return jsonNode.ToJsonString(formatted
                                            ? SystemJsonConverter.DefaultJsonSerializerOptions()
                                            : SystemJsonConverter.DefaultJsonSerializerOptionsNotFormatted());
        }
        else
        {
            return System.Text.Json.JsonSerializer.Serialize(input, formatted
                                            ? SystemJsonConverter.DefaultJsonSerializerOptions()
                                            : SystemJsonConverter.DefaultJsonSerializerOptionsNotFormatted());
        }
    }

    public static T? ParseString<T>(string input)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(input, SystemJsonConverter.DefaultJsonSerializerOptions());
    }

    public static System.Text.Json.Nodes.JsonNode? ParseString(string input)
    {
        return System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonNode>(input, SystemJsonConverter.DefaultJsonSerializerOptions());
    }

}
