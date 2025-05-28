using AngleSharp.Html.Parser;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class HtmlParseNodeImpl : INodeImplement<HtmlParseNode>, INodeImplement
{
    public HtmlParseNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public HtmlParseNodeImpl(HtmlParseNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback)
    {
        if (input.Payload is not string html) throw new Exception("input.Payload must be string");

        var parser = new HtmlParser();
        var dom = parser.ParseDocument(html);
        //var document = parser.ParseFragment(html, dom.Body!);
        var elements = dom.QuerySelectorAll(Node.Selector);

        var result = Array.Empty<object>();

        if (elements.Length == 0)
        {
            if (Node.DontReturnMessageIfNothingFound) return Task.CompletedTask;

            input.Payload = result;
            callback(input);
            return Task.CompletedTask;
        }

        if (Node.Output == HtmlParseNodeOutput.Html)
        {
            result = elements.Select(s => s.InnerHtml).ToArray();
        }
        else if (Node.Output == HtmlParseNodeOutput.Text)
        {
            result = elements.Select(s => s.TextContent).ToArray();
        }
        else if (Node.Output == HtmlParseNodeOutput.MapToObjects)
        {
            var mapObjects = new Dictionary<string, string?>[elements.Length];
            for (var elIndex = 0; elIndex < elements.Length; elIndex++)
            {
                var el = elements[elIndex];
                var obj = new Dictionary<string, string?>(Node.InputMappings.Length, StringComparer.OrdinalIgnoreCase);
                for (var fieldIndex = 0; fieldIndex < Node.InputMappings.Length; fieldIndex++)
                {
                    var field = Node.InputMappings[fieldIndex];
                    var fieldName = string.IsNullOrEmpty(field.OutputField) ? $"field{fieldIndex + 1}" : field.OutputField;
                    var subEl = (string.IsNullOrEmpty(field.Selector) ? el : el.QuerySelector(field.Selector));

                    var val = field.ReturnValue switch
                    {
                        InputMappingReturnValue.Text => subEl.TextContent,
                        InputMappingReturnValue.Html => subEl.InnerHtml,
                        InputMappingReturnValue.Attribute => subEl.GetAttribute(field.Attribute),
                        _ => throw new NotImplementedException()
                    };
                    obj[fieldName] = val;
                }
                mapObjects[elIndex] = obj;
            }
            result = mapObjects;
        }
        else
        {
            throw new NotImplementedException();
        }

        if (!Node.ReturnEachObjectAsMessage)
        {
            input.Payload = result;
            callback(input);
        }
        else
        {
            foreach (var _re in result)
            {
                var x = _re;
                input.Payload = x;
                callback(input);
            }
        }

        return Task.CompletedTask;
    }
}
