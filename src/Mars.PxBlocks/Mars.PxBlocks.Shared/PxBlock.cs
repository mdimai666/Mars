using System.Text.Json.Serialization;

namespace Mars.PxBlocks.Shared;

public abstract class PxBlock
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public virtual string TypeId => GetType().FullName!;

    [JsonIgnore]
    public virtual string Label => GetType().Name.EndsWith("Block")
        ? GetType().Name[..^5]
        : GetType().Name;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float X { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float Y { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float Z { get; set; }

    [JsonIgnore]
    public virtual string DisplayName => Label;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Color { get; set; } = "#A8A8A8";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Disabled { get; set; }

    public List<PxField> Fields { get; set; } = [];
    public List<PxInput> Inputs { get; set; } = [];

    [JsonIgnore]
    public bool IsDragging { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Container { get; set; } = "";
}

public class PxBlockCommand : PxBlock
{
    public string? PreviousBlockId { get; set; }
    public string? NextBlockId { get; set; }

    public PxBlockCommand() { Color = "#5C81A6"; }
}

public class PxBlockHat : PxBlock
{
    public string? NextBlockId { get; set; }

    public PxBlockHat() { Color = "#A5745B"; }
}

public class PxBlockValue : PxBlock
{
    public string OutputType { get; set; } = "string";

    public PxBlockValue() { Color = "#8C5BA5"; }
}
