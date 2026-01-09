using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AppShared.Models;
using BenchmarkDotNet.Attributes;
using Mars.Core.Extensions;
using HandlebarsDotNet;
using Newtonsoft.Json.Linq;

namespace Benchmarks.MyHandlebars;

[MemoryDiagnoser, ShortRunJob]
public class MyHandlebarsMemoryBenchmark
{
    List<Post> posts = default!;
    int[] list = default!;
    string jsonString = default!;

    Dictionary<string, object> context = default!;

    string html = @"
    <div>
        <pre>{{list}}</pre>
        <hr/>
        {{#each posts}}
        <article>
            <h1>{{Title}}</h1>
            <div class=""content"">
                {{{Content}}}
            </div>
            <div>
                {{User.FirstName}}
            </div>
        </article>
        {{/each}}
    </div>
";

    HandlebarsTemplate<object, object> templateCompiled = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        int COUNT = 100;
        string content = Enumerable.Repeat("post content {i}", 20).JoinStr("\n");

        posts = new List<Post>(COUNT);
        for (int i = 0; i < COUNT; i++)
        {
            var post = new Post()
            {
                Id = Guid.Empty,
                Title = $"post title {i}",
                Content = content,
                User = User.GetTest()
            };
            posts.Add(post);
        }
        list = Enumerable.Range(0, 100).ToArray();

        var ctx = new Dictionary<string, object>();
        ctx.TryAdd("post", posts);
        ctx.TryAdd("list", list);
        context = ctx;

        templateCompiled = Handlebars.Compile(html);

        jsonString = JsonSerializer.Serialize(ctx);
    }

    [Benchmark(Baseline = true)]
    public object JsonMemory_SystemText()
    {
        var j = JsonSerializer.Deserialize<JsonObject>(jsonString);
        //var j = new System.Text.Json.Nodes.JsonObject(context);
        return j!;
    }

    [Benchmark]
    public object JsonMemory_NewtonJson()
    {
        //var j = JObject.FromObject(context);
        var j = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(jsonString);
        return j!;
    }

    [Benchmark]
    public object JsonMemory_NewtonJson_FromObject()
    {
        var j = JObject.FromObject(context);
        return j!;
    }

    //[Benchmark]
    //public string HandlebarsCompiled_object()
    //{
    //    string result = templateCompiled(context);
    //    return result;
    //}

    //[Benchmark]
    //public string HandlebarsCompiled_SystemText()
    //{
    //    string result = templateCompiled(context);
    //    return result;
    //}

    //[Benchmark]
    //public string HandlebarsCompiled_NewtonJson()
    //{
    //    string result = templateCompiled(context);
    //    return result;
    //}

    //[Benchmark]
    //public string HandlebarsEachTimeCompile()
    //{
    //    var template = Handlebars.Compile(html);
    //    string result = template(context);
    //    return result;
    //}
}
