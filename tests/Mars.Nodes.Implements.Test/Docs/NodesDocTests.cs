using System.Reflection;
using Mars.Core.Attributes;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using FluentAssertions;

namespace Mars.Nodes.Implements.Test.Docs;

public class NodesDocTests
{
    private string GetMarsPath()
    {
        return Directory.GetCurrentDirectory().Split(@"\Mars\", 2)[0] + @"\Mars\src\";
    }

    public readonly string[] Langs = ["", "ru"];

    record NodeInfo(Type NodeType, FunctionApiDocumentAttribute? Attribute);

    static object _lock = new object();

    IEnumerable<NodeInfo> GetNodes()
    {
        lock (_lock)
        {
            NodesLocator.assemblies.Clear();
            NodesLocator.RegisterAssembly(typeof(InjectNode).Assembly);
            NodesLocator.RefreshDict();
            var nodes = NodesLocator.dict.Values.ToList();
            return nodes.Select(t => new NodeInfo(t, t.GetCustomAttribute<FunctionApiDocumentAttribute>())).ToList();
        }
    }

    [Fact]
    public void Check_FunctionApiDocumentAttribute_Exists()
    {
        var nodes = GetNodes();

        nodes.Count(s => s.Attribute != null).Should().Be(nodes.Count());
    }

    [Fact]
    public void Check_DocsForNodesFiles_Exists()
    {
        var cd = GetMarsPath();

        var nodes = GetNodes();

        var expectDocCount = nodes.Count() * Langs.Length;
        var existFilesList = new List<string>(expectDocCount);

        foreach (var nodeInfo in nodes.Where(s => s.Attribute != null))
        {
            foreach (var lang in Langs)
            {
                var preparedPath = FunctionApiDocumentAttribute.ReplaceLang(nodeInfo.Attribute.Url, lang);
                preparedPath = preparedPath.Replace("_content/NodeFormEditor", "Mars.Nodes\\NodeFormEditor\\wwwroot");

                var dir = Path.GetFullPath(preparedPath, cd);

                if (File.Exists(dir)) existFilesList.Add(dir);
            }
        }

        //Uncomment fo create blank
        //CreateDocsForNodesIfNotExist();

        existFilesList.Count().Should().Be(expectDocCount, "Some doc files missing");
    }

    private void CreateDocsForNodesIfNotExist()
    {
        var cd = Directory.GetCurrentDirectory().Split(@"\Mars\", 2)[0] + @"\Mars\";
        var placeDoc = @"Mars.Nodes\Front\NodeFormEditor\wwwroot\Docs\";
        var fullpath = Path.Join(cd, placeDoc);

        string[] langs = ["", ".ru"];

        //NodesLocator.RegisterAssembly(typeof(InjectNode).Assembly);
        //NodesLocator.RefreshDict();
        var nodes = NodesLocator.dict.Values.Where(s => s.Assembly == typeof(InjectNode).Assembly).ToList();

        foreach (var nodeType in nodes)
        {
            var name = nodeType.Name;
            var dir = Path.Combine(fullpath, name);

            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            foreach (var lang in langs)
            {
                var fn = $"{name}{lang}.md";
                var fdir = Path.Combine(fullpath, name, fn);

                if (!File.Exists(fdir))
                {
                    var content = $"""
                        # {name}
                        Description
                        """;
                    File.WriteAllText(fdir, content);
                }
            }
        }
    }
}
