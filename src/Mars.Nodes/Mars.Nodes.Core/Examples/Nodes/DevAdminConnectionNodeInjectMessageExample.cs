using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class DevAdminConnectionNodeInjectMessageExample : INodeExample<DevAdminConnectionNode>
{
    public string Name => "Inject notify message";
    public string Description => "Inject notify message";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode() { Payload = "Hello!" })
            .AddNext(new DevAdminConnectionNode()
            {
                MessageRecipient = MessageRecipientType.All,
                MessageIntent = Mars.Core.Models.MessageIntent.Success.ToString()
            })
            .Build();
    }
}
