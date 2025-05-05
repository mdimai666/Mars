using System.Dynamic;
using Mars.Host.Templators.HandlebarsFunc;
using HandlebarsDotNet;

namespace Test.Mars.Host.TestHandlebars1;

public class BasicExpressionTests
{
    [Fact]

    public void TestBasicExpressions()
    {
        string htmlTemplateOk = """
            {{#if ok}}
            ok
            {{else}}
            no
            {{/if}}
            """;

        string htmlTemplateNo = """
            {{#unless no }}
            ok
            {{else}}
            no
            {{/unless}}
            """;

        var data = new
        {
            ok = true,
            no = false
        };

        var templateOk = Handlebars.Compile(htmlTemplateOk);
        Assert.Equal("ok", templateOk(data).Trim());

        var templateNo = Handlebars.Compile(htmlTemplateNo);
        Assert.Equal("ok", templateNo(data).Trim());

    }

    [Fact]
    public void TestArrays()
    {
        var data = new
        {
            arr = new string[] { "1", "2", "3", "4" },
        };

        string htmlArrLength = @"{{arr.length}}";

        var templateArrLength = Handlebars.Compile(htmlArrLength);

        Assert.Equal("4", templateArrLength(data).Trim());

        string htmlArrEach = @"{{#each arr}} {{.}} {{/each}}";

        var templateArrEach = Handlebars.Compile(htmlArrEach);

        Assert.Equal("1234", templateArrEach(data).Replace(" ", "").Trim());
    }

    [Fact]
    public void TestExpandoObject()
    {
        var data = new ExpandoObject();
        data.TryAdd("false", false);

        string html = @"{{#unless false}}1{{else}}0{{/unless}}";

        var template = Handlebars.Compile(html);

        Assert.Equal("1", template(data).Trim());
    }

    [Fact]
    public void TestDictionaryObject()
    {
        var data = new Dictionary<string, object>();
        data.TryAdd("false", false);

        string html = @"{{#unless false}}1{{else}}0{{/unless}}";

        var template = Handlebars.Compile(html);

        Assert.Equal("1", template(data).Trim());
    }

    [Fact]
    public void TestCaseSensevityObject()
    {
        var data = new ExpandoObject();
        data.TryAdd("dima", new { False = false, Count = 123 });

        string html1 = @"{{#eq dima.Count 123}}1{{else}}0{{/eq}}";
        string html2 = @"{{#eq dima.count 123}}1{{else}}0{{/eq}}";

        var handlebars = new MyHandlebars();

        var template1 = handlebars.Compile(html1);
        var template2 = handlebars.Compile(html2);

        Assert.Equal(template1(data).Trim(), template2(data).Trim());
    }

    [Fact]
    public void TestCaseSensevityDictionaryObject()
    {
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        data.Add("False", false);
        data.Add("Count", 123);
        //data.Add("count", new { False = false, Count = 123 });

        string html1 = @"{{#eq Count 123}}1{{else}}0{{/eq}}";
        string html2 = @"{{#eq count 123}}1{{else}}0{{/eq}}";

        var handlebars = new MyHandlebars();

        var template1 = handlebars.Compile(html1);
        var template2 = handlebars.Compile(html2);

        Assert.Equal(template1(data).Trim(), template2(data).Trim());
    }

    [Fact]
    public void TestCaseSensevityDictionaryOutput()
    {
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        data.Add("Title", "dima");

        string html1 = @"{{Title}}";
        string html2 = @"{{title}}";

        var handlebars = new MyHandlebars();

        var template1 = handlebars.Compile(html1);
        var template2 = handlebars.Compile(html2);

        Assert.Equal(template1(data).Trim(), template2(data).Trim());
        Assert.NotEqual("", template2(data).Trim());
    }
}
