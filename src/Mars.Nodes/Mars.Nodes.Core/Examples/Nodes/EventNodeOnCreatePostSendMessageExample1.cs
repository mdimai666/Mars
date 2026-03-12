using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class EventNodeOnCreatePostSendMessageExample1 : INodeExample<EventNode>
{
    public string Name => "on create post send message";
    public string Description => "Create text message when create post event";

    public IReadOnlyCollection<Node> Handle()
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new EventNode
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
