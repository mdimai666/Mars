using Mars.Nodes.Core.Nodes.Events;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class EventListenerNodeOnCreatePostSendMessageExample1 : INodeExample<EventListenerNode>
{
    public string Name => "on create post send message";
    public string Description => "Create text message when create post event";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new EventListenerNode
            {
                Topics = "entity.post/post/add"
            })
            .AddNext(new TemplateNode
            {
                Template = """
                Title: {{Payload.data.title}}
                {{#each Payload.data.metaValues }}
                meta-{{metaField.key}}: {{value}}
                {{/each}}
                """
            })
            .AddNext(new DebugNode())
            .Build();
    }
}
