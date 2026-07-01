using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Nodes.Core.Helpers;

namespace Mars.Nodes.Core;

/// <summary>
/// all property field will save in json
/// </summary>
//[JsonConverter(typeof(NodeJsonConverter))] use from DI
public class Node : INodeBasic
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public virtual string Name { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public virtual string TypeId => GetType().FullName!;

    [JsonIgnore]
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
    public virtual string DisplayName => Label;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public virtual string Color { get; set; } = "#A8A8A8";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public virtual string Icon { get; set; } = "";

    List<List<NodeWire>> _wires = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Disabled { get; set; }

    public List<List<NodeWire>> Wires
    {
        get
        {
            if (_wires == null)
            {
                _wires = new List<List<NodeWire>>(Outputs.Count);
                foreach (var w in Outputs)
                {
                    _wires.Add([]);
                }
            }
            return _wires;
        }
        set => _wires = value;
    }

    public List<NodeInput> Inputs { get; set; } = [];
    public List<NodeOutput> Outputs { get; set; } = [];

    public bool enable_status;
    public bool selected;
    public bool changed;
    public bool error;
    public string? status;

    public bool isInjectable;
    public bool hasTailButton;

    // use : NodeJsonConverter
    public virtual Node Copy(JsonSerializerOptions jsonSerializerOptions)
    {
        string json = JsonSerializer.Serialize(this, jsonSerializerOptions);
        Node node = (JsonSerializer.Deserialize(json, GetType(), jsonSerializerOptions) as Node)!;
        return node;
    }

    public virtual Node Copy(INodesLocator nodesLocator)
    {
        var jsonSerializerOptions = nodesLocator.CreateJsonSerializerOptions();

        string json = JsonSerializer.Serialize(this, jsonSerializerOptions);
        Node node = (JsonSerializer.Deserialize(json, GetType(), jsonSerializerOptions) as Node)!;
        return node;
    }

    public static Type[] NonVisualNodes = [typeof(FlowNode), typeof(ConfigNode), typeof(VarNode)];
    public static bool IsVisualNode(Type nodeType) => NodeTypeCache.IsVisualNode(nodeType);

    public static Type[] ContainerlessNodes = [typeof(FlowNode), typeof(ConfigNode), typeof(VarNode)];
    public static bool IsContainerlessNode(Node node) => NodeTypeCache.IsContainerlessNode(node.GetType())
                                                            || node is UnknownNode unknownNode && unknownNode.IsDefinedAsConfig;

    [JsonIgnore]
    public virtual bool IsVisual => IsVisualNode(GetType());

    [JsonIgnore]
    public virtual bool IsContainerless => IsContainerlessNode(this);

    [JsonIgnore]
    public virtual bool IsConfigNode => NodeTypeCache.IsConfigNode(GetType());

    [JsonIgnore]
    public virtual bool IsLinkNode => this is LinkInNode or LinkOutNode;

    [JsonIgnore]
    public int OutputCount
    {

        get => Outputs.Count;
        set
        {
            if (value < 0 || value == Outputs.Count) return;

            while (Wires.Count < Outputs.Count)
                Wires.Add([]);

            while (Wires.Count > Outputs.Count)
                Wires.RemoveAt(Wires.Count - 1);

            while (Outputs.Count < value)
            {
                Outputs.Add(new NodeOutput());
                if (Wires.Count <= Outputs.Count - 1)
                    Wires.Add([]);
            }

            while (Outputs.Count > value)
            {
                int lastIndex = Outputs.Count - 1;
                Outputs.RemoveAt(lastIndex);
                Wires.RemoveAt(lastIndex);
            }
        }
    }

    [JsonIgnore]
    public int InputCount
    {

        get => Inputs.Count;
        set
        {
            if (value < 0 || value == Inputs.Count) return;

            while (Wires.Count < Inputs.Count)
                Wires.Add([]);

            while (Wires.Count > Inputs.Count)
                Wires.RemoveAt(Wires.Count - 1);

            while (Inputs.Count < value)
            {
                Inputs.Add(new NodeInput());
                if (Wires.Count <= Inputs.Count - 1)
                    Wires.Add([]);
            }

            while (Inputs.Count > value)
            {
                int lastIndex = Inputs.Count - 1;
                Inputs.RemoveAt(lastIndex);
                Wires.RemoveAt(lastIndex);
            }
        }
    }
}
