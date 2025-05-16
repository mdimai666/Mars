using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core;

/// <summary>
/// all property field will save in json
/// </summary>
[JsonConverter(typeof(NodeJsonConverter))]
public class Node : INodeBasic
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public virtual string Name { get; set; } = "";
    public string Type => GetType().FullName!;
    public virtual string Label
    {
        get
        {
            string n = GetType().Name;

            return string.IsNullOrWhiteSpace(Name) ? n.Substring(0, n.Length - 4) : Name;
        }
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float X { get; set; } = 0;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float Y { get; set; } = 0;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float Z { get; set; } = 0;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Container { get; set; } = "";

    [JsonIgnore]
    public string DisplayName => string.IsNullOrEmpty(Name) ? Label : Name;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public virtual string Color { get; set; } = "#A8A8A8";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public virtual string Icon { get; set; } = "";

    List<List<string>> _wires = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Disabled { get; set; }


    public List<List<string>> Wires
    {
        get
        {
            if (_wires == null)
            {
                _wires = new List<List<string>>(Outputs.Count);
                foreach (var w in Outputs)
                {
                    _wires.Add(new List<string>());
                }
            }
            return _wires;
        }
        set => _wires = value;
    }

    public List<string> OutputLabels { get; set; } = new List<string>();

    public List<NodeOutput> Outputs { get; set; } = new List<NodeOutput>();

    public bool enable_status;
    public bool selected;
    public bool changed;
    public bool error;
    public string? status;

    public bool isInjectable;
    public bool hasTailButton;
    public bool HaveInput { get; set; }

    //public virtual string GetJson()
    //{
    //    return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    //}

    //public virtual Node FromJson(string jsonString)
    //{
    //    var basic = JsonSerializer.Deserialize<Node>(jsonString);
    //    return basic;
    //}

    public virtual Node Copy()
    {
        string json = JsonSerializer.Serialize(this);
        Node node = (JsonSerializer.Deserialize(json, typeof(Node)) as Node)!;
        node.Id = Guid.NewGuid().ToString();
        return node;
    }

    public static Type[] NonVisualNodes = { typeof(FlowNode), typeof(ConfigNode), typeof(VarNode) };

    public static bool IsVisualNode(Type nodeType) => !NonVisualNodes.Any(t => t == nodeType || t.IsAssignableFrom(nodeType));


    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public virtual bool IsVisual => IsVisualNode(GetType());

    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public virtual bool IsConfigNode => typeof(ConfigNode).IsAssignableFrom(GetType());

    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public int OutputCount
    {

        get => Outputs.Count;
        set
        {
            var Node = this;
            if (value < 0 || value == Node.Outputs.Count) return;
            // Node.Outputs.Capacity = value;
            do
            {
                if (Node.Outputs.Count < value)
                {
                    Node.Outputs.Add(new NodeOutput());
                    Node.Wires.Add(new List<string>());
                }
                else
                {
                    Node.Outputs.RemoveAt(Node.Outputs.Count - 1);
                    Node.Wires.RemoveAt(Node.Outputs.Count - 1);
                }
            } while (Node.Outputs.Count != value);
        }
    }
}
