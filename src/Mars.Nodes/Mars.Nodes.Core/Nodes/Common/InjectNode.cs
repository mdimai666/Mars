using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Common;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/InjectNode/InjectNode{.lang}.md")]
[Display(GroupName = "common")]
public class InjectNode : Node, IValidatableObject, INodeOutputValueSpec
{
    public override string TypeId => "core.InjectNode";

    public const string PayloadKey = "Payload";

    InjectNodeField[] _fields = [new() { Key = PayloadKey, VarType = VarNode.TimestampTypeName }];

    [ValidateComplexType]
    public InjectNodeField[] Fields
    {
        get => _fields;
        set => _fields = value ?? [];
    }

    [Display(Name = "Run at startup")]
    public bool RunAtStartup { get; set; }

    [Display(Name = "Delay millis")]
    public int StartupDelayMillis { get; set; }

    public bool IsSchedule { get; set; }
    public string ScheduleCronMask { get; set; } = "0 0/10 * * * ?";

    public InjectNode()
    {
        isInjectable = true;
        Color = "#A9BBCF";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/box-arrow-in-right.svg";
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Fields.Length == 0)
        {
            yield return new ValidationResult("Add at least one field.", [nameof(Fields)]);
            yield break;
        }

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in Fields)
        {
            if (!IsValidKey(field.Key))
                yield return new ValidationResult($"Key '{field.Key}' must be an identifier or dot path (letter or underscore first per segment).", [nameof(Fields)]);
            else if (!keys.Add(field.Key))
                yield return new ValidationResult($"Key '{field.Key}' is duplicated.", [nameof(Fields)]);

            if (!VarNode.IsValidVarType(field.VarType))
                yield return new ValidationResult($"Type '{field.VarType}' is not supported.", [nameof(Fields)]);

            if (!InputValueKind.IsValid(field.ValueKind))
                yield return new ValidationResult($"Value kind '{field.ValueKind}' is not supported.", [nameof(Fields)]);
            else if (field.ValueKind == InputValueKind.Expression && string.IsNullOrWhiteSpace(field.Value))
                yield return new ValidationResult($"Field '{field.Key}': expression must not be empty.", [nameof(Fields)]);
            else if (field.ValueKind == InputValueKind.Msg && string.IsNullOrWhiteSpace(field.Value))
                yield return new ValidationResult($"Field '{field.Key}': path must not be empty.", [nameof(Fields)]);
        }
    }

    static bool IsValidKey(string? key)
        => !string.IsNullOrEmpty(key)
           && key.Split('.').All(s => s.Length > 0
                                      && (char.IsLetter(s[0]) || s[0] == '_')
                                      && s.All(c => char.IsLetterOrDigit(c) || c == '_'));

    public InjectNode SetPayload(string payload)
    {
        var field = _fields.FirstOrDefault(s => s.Key == PayloadKey);
        if (field == null) _fields = [.. _fields, new() { Key = PayloadKey, Value = payload, VarType = "string" }];
        else { field.Value = payload; field.VarType = "string"; }
        return this;
    }

    public IEnumerable<OutputValueSpec> GetOutputValueSpec()
    {
        foreach (var field in Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Key)) continue;

            yield return new OutputValueSpec(field.Key,
                string.IsNullOrWhiteSpace(field.VarType) ? VarNode.ObjectTypeName : field.VarType);
        }
    }
}

public class InjectNodeField
{
    [Required]
    [Display(Name = "Key")]
    public string Key { get; set; } = "";

    [Display(Name = "Type")]
    public string VarType { get; set; } = "string";

    [Display(Name = "Value kind")]
    public string ValueKind { get; set; } = InputValueKind.Const;

    [Display(Name = "Value")]
    public string Value { get; set; } = "";
}
