namespace Mars.Nodes.Core;

/// <summary>
/// For nodes whose outputs are known from the instance (InjectNode fields, TemplateNode property).
/// Static outputs are declared with <see cref="NodeOutputValueSpecAttribute"/> instead.
/// </summary>
public interface INodeOutputValueSpec
{
    IEnumerable<OutputValueSpec> GetOutputValueSpec();
}
