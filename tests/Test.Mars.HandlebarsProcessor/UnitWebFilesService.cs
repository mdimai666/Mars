using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Models;

namespace Test.Mars.HandlebarsProcessor;

public class UnitWebFilesService
{
    [Fact]
    public void ScanFiles()
    {
        var wfs = new WebFilesReadFilesystemService();

        string path = @"C:\Users\D\Documents\VisualStudio\2023\100web\test_Mars_templator\site\public";

        //var site = wfs.ScanSite(path);

        //Assert.NotNull(site);
        //Assert.True(site.Pages.Count > 0);

        var files = wfs.ScanFiles(path);
        Assert.True(files.Count() > 0);
    }

    [Fact]
    public void TestReadAttribute()
    {
        string thtml = @"
@page ""/""
@layout ""layout1""
@role ""Admin""

{{#context}}
posts = post.Take(10)
@none = asd
{{/context}}

{{#each posts}}

<div>
  @none2 = asd
  <div class=""post_title"">{{title}}</div>
  <div class=""post_content"">{{{content}}}</div>
</div>

{{/each}}";

        WebPartSource wpart = new WebPartSource(thtml, "index.html", "title", "index.html", "index.html");

        WebPage page = new WebPage(wpart);

        Assert.Equal(3, page.Attributes.Count);

        Assert.Equal(@"/", page.Attributes["page"]);
        Assert.Equal(@"layout1", page.Attributes["layout"]);
        Assert.Equal(@"Admin", page.Attributes["role"]);

    }
}
