using System.Dynamic;
using FluentAssertions;
using HandlebarsDotNet;
using Mars.Host.Templators.HandlebarsFunc;

namespace Test.Mars.WebSiteProcessor.Templators.HandlebarsEngine;

public class BasicExpressionTests
{
    [Fact]

    public void IfBlock_Renders_WorksCorrectly()
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
        templateOk(data).Trim().Should().Be("ok");

        var templateNo = Handlebars.Compile(htmlTemplateNo);
        templateNo(data).Trim().Should().Be("ok");
    }

    [Fact]
    public void EachBlock_Renders_WorksCorrectly()
    {
        var data = new
        {
            arr = new string[] { "1", "2", "3", "4" },
        };

        string htmlArrLength = @"{{arr.length}}";

        var templateArrLength = Handlebars.Compile(htmlArrLength);

        templateArrLength(data).Trim().Should().Be("4");

        string htmlArrEach = @"{{#each arr}} {{.}} {{/each}}";

        var templateArrEach = Handlebars.Compile(htmlArrEach);

        templateArrEach(data).Trim().Should().Be("1  2  3  4");
    }

    [Fact]
    public void ExpandoObject_BoolField_ShouldRecognizeCorrect()
    {
        var data = new ExpandoObject();
        data.TryAdd("false", false);

        string html = @"{{#unless false}}1{{else}}0{{/unless}}";

        var template = Handlebars.Compile(html);

        template(data).Trim().Should().Be("1");
    }

    [Fact]
    public void DictionaryObject_BoolField_ShouldRecognizeCorrect()
    {
        var data = new Dictionary<string, object>();
        data.TryAdd("false", false);

        string html = @"{{#unless false}}1{{else}}0{{/unless}}";

        var template = Handlebars.Compile(html);

        template(data).Trim().Should().Be("1");
    }

    [Fact]
    public void EqBlock_ExpandoObjectCaseInsensevity_ShouldWorkExpect()
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
    public void EqBlock_DictionaryCaseInsensevity_ShouldWorkExpect()
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
    public void OutputVariable_CaseInsensetive_ShouldWorkExpect()
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
