using System.Text.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using BenchmarkDotNet.Attributes;
using HandlebarsDotNet;
using Mars.Core.Extensions;
using Mars.Host.Shared.Dto.Posts;
using Newtonsoft.Json.Linq;

namespace Benchmarks.MyHandlebars;

[MemoryDiagnoser, ShortRunJob]
public class MyHandlebarsMemoryBenchmark
{
    List<PostDetail> posts = default!;
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
        var fixture = new Fixture();
        posts = fixture.CreateMany<PostDetail>(COUNT).ToList();
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
