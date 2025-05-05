using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppShared.Models;
using BenchmarkDotNet.Attributes;
using HandlebarsDotNet;

namespace Benchmarks.MyHandlebars;

[MemoryDiagnoser, ShortRunJob]
public class MyHandlebarsCompileBenchmark
{
    Post? post;
    int[] list = default!;

    object context = new { };

    string html = @"
    <div>
        <article>
            <h1>{{post.Title}}</h1>
            <div class=""content"">
                {{{post.Content}}}
            </div>
        </article>
    </div>
";

    HandlebarsTemplate<object, object> templateCompiled = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        post = new Post()
        {
            Id = Guid.Empty,
            Title = "post title1",
            Content = "post content",
        };
        list = Enumerable.Range(0, 100).ToArray();

        var ctx = new ExpandoObject();
        ctx.TryAdd("post", post);
        ctx.TryAdd("list", list);
        context = ctx;

        templateCompiled = Handlebars.Compile(html);

    }

    [Benchmark(Baseline = true)]
    public string NativeReplace()
    {
        string result = html
                .Replace("{{post.Title}}", post.Title)
                .Replace("{{post.Content}}", post.Content);
        return result;
    }

    [Benchmark]
    public string HandlebarsCompiled()
    {
        string result = templateCompiled(context);
        return result;
    }

    [Benchmark]
    public string HandlebarsEachTimeCompile()
    {
        var template = Handlebars.Compile(html);
        string result = template(context);
        return result;
    }
}
