using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Core.Nodes.Functions;

namespace Mars.Nodes.Workspace.Models;

public static class PaletteBuilder
{
    static readonly Type[] TopNodes = [typeof(InjectNode), typeof(DebugNode), typeof(FunctionNode), typeof(TemplateNode)];

    public static List<PaletteNode> Build(IEnumerable<Type> registeredNodes,
                                          IReadOnlyDictionary<string, InlineFunctionNodeSchema> inlineFunctionNodeSchemas)
    {
        var palette = new List<PaletteNode>();

        var paletteNodeTypes = TopNodes.Concat(
            registeredNodes
                .Where(s => Node.IsVisualNode(s) && s != typeof(UnknownNode))
                .Where(s => !TopNodes.Contains(s)));

        foreach (var type in paletteNodeTypes)
        {
            var node = (Node)Activator.CreateInstance(type)!;
            node.X = 10;
            var displayAttr = type.GetCustomAttribute<DisplayAttribute>();
            palette.Add(new PaletteNode
            {
                Instance = node,
                DisplayName = node.Label,
                GroupName = displayAttr?.GroupName ?? "other"
            });
        }

        foreach (var inlineNodeDef in inlineFunctionNodeSchemas.Values)
        {
            var node = InlineFunctionNode.CreateInlineFunctionNode(inlineNodeDef);
            palette.Add(new PaletteNode
            {
                Instance = node,
                DisplayName = node.Label,
                GroupName = inlineNodeDef.GroupName
            });
        }

        return palette;
    }
}
