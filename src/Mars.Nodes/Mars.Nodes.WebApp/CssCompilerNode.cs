using Mars.Nodes.Core;

namespace Mars.Nodes.WebApp;

public class CssCompilerNode : Node
{
    public CssCompilerNode()
    {
        Color = "#4f9ad5";
        haveInput = true;
        Outputs = new List<NodeOutput> { new NodeOutput() { Label = "compiled css" } };
    }
}
