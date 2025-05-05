using Mars.Host.Data;
using Mars.Host.Shared.WebSite.Models;
using Mars.Host.Templators.HandlebarsFunc;
using HandlebarsDotNet;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Test.Mars.Host.TestHandlebars1;

#if false
public class UnitTestMarsTemplator : UnitTestHostBaseClass
{

    (RenderEngineRenderRequestContext pctx, List<string> errors, IHandlebars handlebars) ctx()
    {
        //var pctx = _pctx();

        RenderEngineRenderRequestContext ctx = _renderContext();

        //List<string> errors = new();
        //pctx.addErr = err => errors.Add(err);

        var handlebars = new MyHandlebars();

        handlebars.RegisterContextFunctions();

        return (ctx, ctx.PageContext.Errors, handlebars.handlebars);
    }

    [Fact]
    public void BasicTest()
    {
        var (pctx, errors, handlebars) = ctx();

        string html = @"{{#unless post}}ok{{else}}post is null{{/unless}}";

        var template = handlebars.Compile(html);

        Assert.Equal("ok", template(pctx.PageContext.templateContext, pctx.AsContextData()).Trim());

        Assert.Empty(errors);

    }

    [Fact]
    public void TestContextHelper()
    {
        var (pctx, errors, handlebars) = ctx();

        string html =
            @"{{#context}} 
                    posts = testPost.Where(post.Title != ""11"").OrderBy(Created).Take(10);
              {{/context}}";

        var template = handlebars.Compile(html);

        var result = template(pctx.PageContext.templateContext, pctx.AsContextData());
        //Assert.Equal("ok", .Trim());

        var jsPosts = pctx.PageContext.templateContext["posts"] as JArray;

        var jsPostsIds = jsPosts.Select(s => s["Id"].ToString()).ToList();

        var ef = _serviceProvider.GetRequiredService<MarsDbContextLegacy>();

        var dbPosts = ef.Posts.Where(s => s.Type == "testPost")
                .Where(s => s.Title != "11")
                .OrderBy(s => s.Created)
                .Take(10)
                .ToList();

        var dbPostsIds = dbPosts.Select(s => s.Id.ToString()).ToList();

        Assert.Equal(dbPostsIds, jsPostsIds);

        Assert.Empty(errors);
    }
} 
#endif
