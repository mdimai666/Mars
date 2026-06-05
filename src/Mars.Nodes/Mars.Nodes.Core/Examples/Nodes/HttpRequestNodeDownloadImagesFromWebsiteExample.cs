using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class HttpRequestNodeDownloadImagesFromWebsiteExample : INodeExample<HttpRequestNode>
{
    public string Name => "Download images from website";
    public string Description => "";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        var builder = NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(new HttpRequestNode() { Url = "https://habr.com/ru/feed/" })
            .AddNext(new VariableSetNode()
            {
                Name = "save request info",
                Setters = [new() { Operation = VariableSetOperation.Set, ValuePath = "msg.query", Expression = "msg.HttpRequestInfo" }]
            })
            .AddNext(new HtmlParseNode()
            {
                Selector = "article",
                Output = HtmlParseNodeOutput.MapToObjects,
                InputMappings = [
                    new() { Selector = "h2.tm-title", ReturnValue = InputMappingReturnValue.Text, OutputField = "title", },
                    new() { Selector = "img.lead-image", ReturnValue = InputMappingReturnValue.Attribute, Attribute = "src", OutputField = "image"},
                    new() { Selector = "h2.tm-title a", ReturnValue = InputMappingReturnValue.Attribute, Attribute = "href", OutputField = "link"}
                ],
                ReturnEachObjectAsMessage = true
            })
            .AddNext(new QueueNode() { MaxTask = 5 })
            .AddNext(NodesWorkflowBuilder.Create()
                        .AddNext(new TemplateNode { Name = "FINISH", Template = "FINISH" })
                        .AddNext(new DebugNode()),
                    NodesWorkflowBuilder.Create()
                        .AddNext(new FunctionNode
                        {
                            Name = "Set file name",
                            Code = """
                                    using System.IO;

                                    var d = (Dictionary<string,string>)msg.Payload;

                                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                                    string downloadsPath = Path.Combine(userProfile, "Downloads");
                                    string lastUrlPath =  ((string)msg.query.Path).ToString().Trim('/').Split('/').Last();

                                    var imgName = Path.GetFileName(d["image"]);
                                    var file = Path.Combine(downloadsPath, "mars_downloaded", lastUrlPath, imgName);

                                    d["file"] = file;

                                    msg.post = d;

                                    return msg;
                                    """
                        })
                        .AddNext(new DebugNode(),
                                new HttpRequestNode() { Method = "GET", Url = "@msg.Payload[\"image\"]" }))
                        .AddNext(new FileWriteNode { FilePath = "@msg[\"post\"][\"file\"]" })
            ;

        var fileWriteNode = builder.Nodes.First(s => s is FileWriteNode);
        var queueNode = builder.Nodes.First(s => s is QueueNode);
        var functionIterateNode = builder.Nodes.First(s => s is FunctionNode);
        var imageRequestNode = builder.Nodes.OfType<HttpRequestNode>().ElementAt(1);

        fileWriteNode.Wires = [[new NodeWire(queueNode.Id, PortIndex: 1)]];
        queueNode.Wires = [[queueNode.Wires[0][0]], [functionIterateNode.Id]];

        var nodes = builder.Build();
        fileWriteNode.X = functionIterateNode.X;
        fileWriteNode.Y += 220;
        imageRequestNode.X = functionIterateNode.X;

        for (int i = 3; i < nodes.Length; i++)
        {
            var node = nodes[i];
            node.X -= 700;
            node.Y += 150;
        }

        return nodes;
    }
}
