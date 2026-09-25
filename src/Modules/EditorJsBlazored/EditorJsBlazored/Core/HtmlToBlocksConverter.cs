using System.Net;
using System.Text;
using EditorJsBlazored.Blocks;
using HtmlAgilityPack;

namespace EditorJsBlazored.Core;

/// <summary>
/// Разбор HTML-разметки в блоки Editor.js.
/// </summary>
/// <remarks>
/// Блочные теги маппятся на зарегистрированные в <see cref="EditorTools.RegisteredBlocks"/> типы,
/// контейнеры (<c>div</c>, <c>section</c>, …) обходятся рекурсивно, инлайновые теги группируются
/// в paragraph, всё нераспознанное уходит в raw.
/// </remarks>
public static class HtmlToBlocksConverter
{
    static readonly HashSet<string> ContainerTags =
    [
        "html", "body", "div", "section", "article", "main", "aside", "header", "footer", "figure",
    ];

    static readonly HashSet<string> InlineTags =
    [
        "a", "abbr", "b", "bdi", "bdo", "big", "br", "cite", "code", "data", "del", "em", "font",
        "i", "ins", "kbd", "label", "mark", "q", "ruby", "s", "samp", "small", "span", "strike",
        "strong", "sub", "sup", "time", "tt", "u", "var", "wbr",
    ];

    public static EditorContentBlock[] Convert(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return [];

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var root = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;

        var blocks = new List<EditorContentBlock>();
        var inline = new StringBuilder();

        Collect(root, blocks, inline);
        Flush(blocks, inline);

        return blocks.Count > 0 ? blocks.ToArray() : [Raw(html)];
    }

    static void Collect(HtmlNode parent, List<EditorContentBlock> blocks, StringBuilder inline)
    {
        foreach (var node in parent.ChildNodes)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    continue;

                case HtmlNodeType.Text:
                    inline.Append(WebUtility.HtmlEncode(HtmlEntity.DeEntitize(node.InnerText)));
                    continue;

                case HtmlNodeType.Element:
                    if (ContainerTags.Contains(node.Name))
                    {
                        Flush(blocks, inline);
                        Collect(node, blocks, inline);
                        Flush(blocks, inline);
                        continue;
                    }

                    var block = MapElement(node);
                    if (block is null)
                    {
                        inline.Append(node.OuterHtml);
                        continue;
                    }

                    Flush(blocks, inline);
                    blocks.Add(block);
                    continue;
            }
        }
    }

    /// <returns>null для инлайновых тегов — их накапливает вызывающий код</returns>
    static EditorContentBlock? MapElement(HtmlNode node)
    {
        var name = node.Name;

        switch (name)
        {
            case "p": return Paragraph(node.InnerHtml.Trim());
            case "ul": return ListBlock(node, "unordered");
            case "ol": return ListBlock(node, "ordered");
            case "blockquote": return Quote(node.InnerHtml.Trim());
            case "pre": return Code(node);
            case "hr": return Delimiter();
            case "img": return Image(node);
            case "table": return Table(node);
        }

        if (name.Length == 2 && name[0] == 'h' && name[1] is >= '1' and <= '6')
            return Header(node.InnerHtml.Trim(), name[1] - '0');

        if (InlineTags.Contains(name)) return null;

        return Raw(node.OuterHtml);
    }

    static void Flush(List<EditorContentBlock> blocks, StringBuilder inline)
    {
        var text = inline.ToString();
        inline.Clear();

        if (string.IsNullOrWhiteSpace(text)) return;

        blocks.Add(Paragraph(text.Trim()));
    }

    static EditorContentBlock Paragraph(string text) => new()
    {
        Type = "paragraph",
        Data = new BlockParagraph { Text = text },
    };

    static EditorContentBlock Header(string text, int level) => new()
    {
        Type = "header",
        Data = new BlockHeader { Text = text, Level = level },
    };

    static EditorContentBlock ListBlock(HtmlNode node, string style) => new()
    {
        Type = "list",
        Data = new BlockList
        {
            Style = style,
            Items = node.ChildNodes
                        .Where(n => n.Name == "li")
                        .Select(n => n.InnerHtml.Trim())
                        .ToArray(),
        },
    };

    static EditorContentBlock Quote(string text) => new()
    {
        Type = "quote",
        Data = new BlockQuote { Text = text, Caption = "", Alignment = "left" },
    };

    static EditorContentBlock Code(HtmlNode node)
    {
        var code = node.ChildNodes.FirstOrDefault(n => n.Name == "code");
        return new EditorContentBlock
        {
            Type = "code",
            // блок code хранит plain text, поэтому сущности должны быть раскрыты
            Data = new BlockCode { Code = HtmlEntity.DeEntitize((code ?? node).InnerText) },
        };
    }

    static EditorContentBlock Delimiter() => new()
    {
        Type = "delimiter",
        Data = new BlockDelimiter(),
    };

    static EditorContentBlock Image(HtmlNode node) => new()
    {
        Type = "image",
        Data = new BlockImage
        {
            File = new BlockImage.ImageFileData { Url = node.GetAttributeValue("src", "") },
            Caption = node.GetAttributeValue("alt", ""),
        },
    };

    static EditorContentBlock Table(HtmlNode node)
    {
        var rows = node.Descendants("tr")
                       .Select(tr => tr.ChildNodes
                                       .Where(c => c.Name is "td" or "th")
                                       .Select(c => c.InnerHtml.Trim())
                                       .ToArray())
                       .Where(r => r.Length > 0)
                       .ToArray();

        return new EditorContentBlock
        {
            Type = "table",
            Data = new BlockTable
            {
                Content = rows,
                Rows = rows.Length,
                Cols = rows.Length > 0 ? rows.Max(r => r.Length) : 0,
                WithHeading = node.Descendants("th").Any(),
            },
        };
    }

    static EditorContentBlock Raw(string html) => new()
    {
        Type = "raw",
        Data = new BlockRaw { Html = html },
    };
}
