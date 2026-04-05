using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class ActionCommandNodeCreateButtonInAboutSysPageExample : INodeExample<ActionCommandNode>
{
    public string Name => "Create button in Settings/AboutSystem page";
    public string Description => "Create button in Settings/AboutSystem page";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        int nodesCount = editorState.Nodes.Length;
        return NodesWorkflowBuilder.Create()
            .AddNext(new ActionCommandNode()
            {
                Name = "Command example " + nodesCount,
                FrontContextId = ["AppAdmin.Pages.Settings.SettingsAboutSystemPage"]
            }, new InjectNode())
            .AddNext(new TemplateNode { Name = "Clicked message", Template = $"ActionCommandNode {nodesCount} clicked!" })
            .AddNext(new DevAdminConnectionNode()
            {
                MessageRecipient = MessageRecipientType.All
            })
            .Build();
    }
}
