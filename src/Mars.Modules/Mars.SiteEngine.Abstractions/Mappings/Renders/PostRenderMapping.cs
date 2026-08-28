using Mars.SiteEngine.Abstractions.Dto.Renders;
using Mars.Contracts.Common;
using Mars.SiteEngine.Contracts.Renders;

namespace Mars.SiteEngine.Abstractions.Mappings.Renders;

public static class PostRenderMapping
{
    public static PostRenderResponse ToResponse(this PostRenderDto entity)
        => new()
        {
            Title = entity.Title,
            DataId = entity.DataId,
            Date = entity.Date,
            EditUrl = entity.EditUrl,
            Html = entity.Html,
            PostSlug = entity.PostSlug,
            PostType = entity.PostType,
            TemplateId = entity.TemplateId,
        };

    public static RenderActionResult<PostRenderResponse> ToResponse(this RenderActionResult<PostRenderDto> entity)
        => new()
        {
            Message = entity.Message,
            Data = entity.Data?.ToResponse()!,
            NotFound = entity.NotFound,
            Ok = entity.Ok,
        };
}
