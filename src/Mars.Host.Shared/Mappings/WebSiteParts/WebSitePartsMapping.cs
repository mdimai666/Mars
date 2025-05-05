using Mars.Host.Shared.WebSite.Models;
using Mars.Shared.Contracts.WebSite.Dto;

namespace Mars.Host.Shared.Mappings.NavMenus;

public static class WebSitePartsMapping
{
    public static FMarsAppFrontTemplateSummaryResponse ToSummaryResponse(this WebSiteTemplate entity)
        => new()
        {
            RootPage = entity.RootPage.ToResponse(),
            Pages = entity.Pages.ToResponse(),
            Parts = entity.Parts.ToResponse(),
            Layouts = entity.Layouts.ToResponse(),
            Page404FileRelPath = entity.Page404?.FileRelPath,
            Page500FileRelPath = entity.Page500?.FileRelPath,
            IndexPageFileRelPath = entity.IndexPage.FileRelPath,
        };

    public static FMarsAppFrontTemplateMinimumResponse ToMinimumResponse(this WebSiteTemplate entity)
        => new()
        {
            RootPage = entity.RootPage.ToResponse(),
            Pages = entity.Pages.ToResponse(),
            Page404FileRelPath = entity.Page404?.FileRelPath,
            Page500FileRelPath = entity.Page500?.FileRelPath,
            IndexPageFileRelPath = entity.IndexPage.FileRelPath,
        };

    public static FWebPartSummaryResponse ToSummaryResponse(this WebSitePart entity)
        => new()
        {
            Type = entity.Type,
            FileName = entity.FileName,
            Title = entity.Title ?? entity.Name,
            FileRelPath = entity.FileRelPath,
            Attributes = entity.Attributes,
        };

    public static FWebPartResponse ToResponse(this WebSitePart entity)
        => new()
        {
            Type = entity.Type,
            FileName = entity.FileName,
            Title = entity.Title ?? entity.Name,
            FileRelPath = entity.FileRelPath,
            Attributes = entity.Attributes,

            Content = entity.Content,
        };

    public static FWebPartResponse ToPartResponse(this WebPage entity)
        => new()
        {
            Type = entity.Type,
            FileName = entity.FileName,
            Title = entity.Title ?? entity.Name,
            FileRelPath = entity.FileRelPath,
            Attributes = entity.Attributes,

            Content = entity.Content,
            //Url = entity.Url,
            //Layout = entity.Layout,
        };

    public static FWebPageSummaryResponse ToSummaryResponse(this WebPage entity)
        => new()
        {
            Type = entity.Type,
            FileName = entity.FileName,
            Title = entity.Title ?? entity.Name,
            FileRelPath = entity.FileRelPath,
            Attributes = entity.Attributes,

            //Content = entity.Content,
            Url = entity.Url,
            Layout = entity.Layout,
        };

    public static FWebPageResponse ToResponse(this WebPage entity)
        => new()
        {
            Type = entity.Type,
            FileName = entity.FileName,
            Title = entity.Title ?? entity.Name,
            FileRelPath = entity.FileRelPath,
            Attributes = entity.Attributes,

            Content = entity.Content,
            Url = entity.Url,
            Layout = entity.Layout,
        };

    public static IReadOnlyCollection<FWebPartSummaryResponse> ToResponse(this IReadOnlyCollection<WebSitePart> list)
        => list.Select(ToSummaryResponse).ToList();

    public static IReadOnlyCollection<FWebPageSummaryResponse> ToResponse(this IReadOnlyCollection<WebPage> list)
        => list.Select(ToSummaryResponse).ToList();
}
