using Mars.Core.Attributes;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;

namespace Mars.SemanticKernel.Shared.Nodes;

//https://learn.microsoft.com/ru-ru/semantic-kernel/get-started/quick-start-guide?pivots=programming-language-csharp
[FunctionApiDocument("./_content/Mars.SemanticKernel.Front/docs/nodes/AIRequestNode/AIRequestNode{.lang}.md")]
public class AIRequestNode : Node
{
    public override string Label => string.IsNullOrEmpty(Name) ? "AI" : Name;


    public InputConfig<SemanticKernelModelConfigNode> Config { get; set; }
    public string Prompt { get; set; } = "";
    public float? Temperature { get; set; }
    public int? TopK { get; set; }
    public float? TopP { get; set; }


    public AIRequestNode()
    {
        HaveInput = true;
        Color = "#aeb3fa";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.SemanticKernel.Front/img/icon-128.png";
    }
}
