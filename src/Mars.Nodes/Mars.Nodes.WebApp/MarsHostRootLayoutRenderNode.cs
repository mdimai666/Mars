using Mars.Nodes.Core;

namespace Mars.Nodes.WebApp;

public class MarsHostRootLayoutRenderNode: Node
{
	public MarsHostRootLayoutRenderNode()
	{
		HaveInput = true;
		Color = "#603dd7";
        Icon = "_content/Mars.Nodes.Workspace/nodes/razor-48.png";
		Outputs = new List<NodeOutput> { new NodeOutput { Label = "html" } };
    }
}

public class RenderPageNode: Node
{

    public string PageIdOrSlug { get; set; } = "";

	public RenderPageNode()
	{
        HaveInput = true;
        Color = "#603dd7";
        Icon = "_content/Mars.Nodes.Workspace/nodes/razor-48.png";
        Outputs = new List<NodeOutput> { new NodeOutput { Label = "html" } };
    }
}

public class RendeFrameNode
{

}
