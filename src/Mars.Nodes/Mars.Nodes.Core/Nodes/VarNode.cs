using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/VarNode/VarNode{.lang}.md")]
public class VarNode : Node, IValidatableObject
{
    public string VarType { get; set; } = "int";
    public string DefaultValue { get; set; } = "0";
    //public bool StoreInRemoteStorage { get; set; }

    bool? _arrayValue;
    public bool ArrayValue { get => _arrayValue ??= ParseVarTypeString(VarType).isArray; }

    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public object? Value { get; set; } = 0;

    public override string Label => $"{VarType}: {Name}";

    [Required]
    [SlugString]
    public override string Name { get; set; } = "";

    public VarNode()
    {
        Color = "#3b71ea";

    }

    internal static readonly Dictionary<string, Type> _typesDict = new()
    {
        ["int"] = typeof(int),
        ["long"] = typeof(long),
        ["float"] = typeof(float),
        ["double"] = typeof(double),
        ["decimal"] = typeof(decimal),
        ["bool"] = typeof(bool),
        ["string"] = typeof(string),
        ["DateTime"] = typeof(DateTime),
        ["Guid"] = typeof(Guid),
    };

    internal static readonly Dictionary<Type, string> _pureArrayInitsDict = new()
    {
        [typeof(int[])] = "int[]",
        [typeof(long[])] = "long[]",
        [typeof(float[])] = "float[]",
        [typeof(double[])] = "double[]",
        //[typeof(decimal[])] = "decimal[]", not work on C#
        [typeof(bool[])] = "bool[]",
        [typeof(string[])] = "string[]",
    };

    public static Type ResolveClrType(string varType)
    {
        if (_typesDict.TryGetValue(varType, out var _type)) return _type;

        var (t, arr) = ParseVarTypeString(varType);
        if (!arr) return _typesDict[t];
        return _typesDict[t].MakeArrayType();
    }

    public static object ResolveDefault(string varType)
    {
        var (t, arr) = ParseVarTypeString(varType);
        if (arr) return Array.CreateInstanceFromArrayType(_typesDict[t].MakeArrayType(), 0);
        //var underlyingType = Nullable.GetUnderlyingType(type);
        return GetTypeDefault(t);
    }

    internal static object GetTypeDefault(string varTypeNonArray)
    {
        return varTypeNonArray switch
        {
            "int" => 0,
            "long" => 0L,
            "float" => 0f,
            "double" => 0d,
            "decimal" => 0M,
            "bool" => false,
            "string" => "",
            "DateTime" => DateTime.MinValue,
            "Guid" => Guid.Empty,
            _ => throw new NotImplementedException()
        };
    }

    public static string? GetPureArrayInitializerPrefix(Type? type)
        => _pureArrayInitsDict.GetValueOrDefault(type!);

    internal static (string varType, bool isArray) ParseVarTypeString(string varType)
    {
        var sp = varType.Split('[', 2);
        if (sp.Length == 1) return (varType, false);

        if (!_typesDict.ContainsKey(sp[0])) throw new NotImplementedException();

        return (sp[0], true);
    }

    internal static bool IsValidVarType(string varType)
    {
        if (_typesDict.ContainsKey(varType)) return true;
        var sp = varType.Split('[', 2);
        return _typesDict.ContainsKey(sp[0]);
    }

    public object GetDefault()
    {
        return ResolveDefault(VarType);
    }

    static string[]? _listTypesSelect;
    public static string[] ListTypesSelect()
    {
        return _listTypesSelect ??= [
            .._typesDict.Keys,
            .._typesDict.Keys.Select(s=>$"{s}[]")
        ];
    }

    public void SetByJsonString(string value)
    {
        Value = JsonSerializer.Deserialize(value, ResolveClrType(VarType)) ?? default(int);
    }

    public void SetValue(object? value)
    {
        var type = ResolveClrType(VarType);
        var isPrimitive = !ArrayValue && type.IsPrimitive;

        if (isPrimitive && value is null) throw new InvalidOperationException("null - not valid type for primitive");

        if (value?.GetType() == type)
        {
            this.Value = value;
        }
        else
        {
            throw new InvalidOperationException($"not same type Declared='{type.Name}' Recived='{value?.GetType()?.Name}'");
        }
    }

    public void TrySetValue(object? value)
    {
        try
        {
            SetValue(value);
        }
        catch
        {
        }
    }

    public VarNodeVaribleDto GetDto()
        => new(VarType, Value, ArrayValue);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (VarType == "string" && !DefaultValue.StartsWith('\"') && !DefaultValue.EndsWith('\"'))
        {
            yield return new ValidationResult("must be in quote '\"' like \"text\"", [nameof(DefaultValue)]);
        }
    }
}

public record VarNodeVaribleDto(string VarType, object? Value, bool ArrayValue);
