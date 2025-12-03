using Mars.Core.Models;
using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.Shared.Options;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.WebSite.SourceProviders;

public class WebTemplateDatabaseSource : IWebTemplateSource
{
    public bool IsFileSystem => false;

    readonly IOptionService optionService;
    readonly MarsAppFront appFront;
    readonly Func<MarsDbContext> getEf;

    public WebTemplateDatabaseSource(IOptionService optionService, MarsAppFront appFront, Func<MarsDbContext> getEf)
    {
        this.optionService = optionService;
        this.appFront = appFront;
        this.getEf = getEf;
    }

    public static string[] activeTypeNames = { "page", "template", "layout", "block" };

    public IEnumerable<WebPartSource> ReadParts()
    {

        //var ef = serviceProvider.GetRequiredService<MarsDbContext>();
        //После EventManager не успевает получить измененную запись.
        //using var ef = MarsDbContext.CreateInstance();
        using var ef = getEf();

        var posts = ef.Posts
            .Include(s => s.PostType)
            .Include(s => s.MetaValues!)
                .ThenInclude(s => s.MetaField)
            .Where(s => activeTypeNames.Contains(s.PostType!.TypeName))
            .ToList();

        if (appFront.Configuration.Mode is AppFrontMode.HandlebarsTemplate or AppFrontMode.None or AppFrontMode.BlazorPrerender)
        {
            var frontOpt = optionService.GetOption<FrontOptions>();
            var frontHost = frontOpt.HostItems.FirstOrDefault(s => s.Url == appFront.Configuration.Url);

            yield return new WebPartSource(frontHost?.HostHtml ?? "@Body", "_root", "RootHtml", "_root.hbs", "_root.hbs");
        }

        foreach (var post in posts)
        {
            string content = post.Content!;

            if (post.PostType.TypeName == "page")
            {
                string? pageUrl = post.MetaValues!
                    .FirstOrDefault(s => s.MetaField.Key == "url" && s.MetaField.ParentId == Guid.Empty)?.Get().ToString();

                if (string.IsNullOrWhiteSpace(pageUrl))
                {
                    pageUrl = "/" + post.Slug;
                }

                if (post.Slug == "index")
                {
                    content = "@page \"/\"\n\n" + content;
                }
                else if (post.Slug == "404")
                {
                    content = "@page \"/404\"\n\n" + content;
                }
                else if (post.Slug == "500")
                {
                    content = "@page \"/500\"\n\n" + content;
                }
                else
                {
                    content = $"@page \"{pageUrl}\"\n\n" + content;
                }
            }
            else if (post.PostType.TypeName == "layout")
            {
                content = "@inherits LayoutComponentBase\n\n" + content;
            }

            yield return new WebPartSource(content, post.Slug, post.Title, post.Slug, post.Slug);
        }
    }
}
