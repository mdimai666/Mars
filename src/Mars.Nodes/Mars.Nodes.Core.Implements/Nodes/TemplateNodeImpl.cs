using HandlebarsDotNet;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class TemplateNodeImpl : INodeImplement<TemplateNode>, INodeImplement
{
    public TemplateNodeImpl(TemplateNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public TemplateNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    HandlebarsTemplate<object, object>? template;
    string? compiled_template;

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        //https://github.com/Handlebars-Net/Handlebars.Net
        if (template is null || compiled_template is null || !Node.Template.Equals(compiled_template))
        {
            template = Handlebars.Compile(Node.Template);
            compiled_template = Node.Template;
        }

        input.Payload = template(input);

        callback(input);

        return Task.CompletedTask;
    }
}
