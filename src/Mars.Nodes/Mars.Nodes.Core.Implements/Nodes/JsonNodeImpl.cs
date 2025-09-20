using System.Dynamic;
using System.Text.Json;
using Mars.Core.Features.JsonConverter;
using Mars.Nodes.Core.Nodes;
using Newtonsoft.Json;

namespace Mars.Nodes.Core.Implements.Nodes;

public class JsonNodeImpl : INodeImplement<JsonNode>, INodeImplement
{
    public JsonNodeImpl(JsonNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public JsonNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (input.Payload is null)
        {
            callback(input);
            return Task.CompletedTask;
        }

        object payload = input.Payload;

        if (Node.Action == JsonNode.JsonNodeAction.toJsonString)
        {
            string json = JsonNodeImpl.ToJsonString(payload, true);
            input.Payload = json;
        }
        else if (Node.Action == JsonNode.JsonNodeAction.toObject)
        {
            var v = JsonNodeImpl.ParseString(payload.ToString()!);
            var obj = v.Deserialize<ExpandoObject>()!;
            input.Payload = obj;
        }
        else
        {
            bool isString = payload is string;
            if (isString)
            {
                //var v = JsonNodeImpl.ParseString(payload.ToString()!);
                //var obj = v.Deserialize<ExpandoObject>()!;
                var v = JsonNodeImpl.ParseString(payload.ToString()!);
                input.Payload = v;
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

    static JsonSerializerSettings _defaultConvertSetting = new JsonSerializerSettings
    {
        Formatting = Formatting.None,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    static JsonSerializerSettings _defaultConvertSettingNotFormatted = new JsonSerializerSettings
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
