using Mars.Core.Extensions;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.Posts;

namespace Mars.Host.Shared.Mappings.Posts;

public static class PostJsonMapping
{
    public static PostJsonDto ToJsonDto(this PostDetail entity, MetaFieldRelatedFillDict? fillDict)
        => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Type = entity.Type,
            Slug = entity.Slug,
            Author = entity.Author,
            Content = entity.Content,
            Tags = entity.Tags,

            ///<see cref="MetaValueMapping.ToDto"/>
            ///<see href="Mars\Mars.Host.Repositories\Mappings\MetaFieldMapping.cs"/>
            Meta = entity.MetaValues.ToDictionary(s => s.MetaField!.Key, v => ConvertObjectValue(v, fillDict)),
            Status = entity.Status,
        };

    public static PostJsonDto ToJsonDtoSummary(this PostDetail entity, MetaFieldRelatedFillDict? fillDict)
        => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Type = entity.Type,
            Slug = entity.Slug,
            Author = entity.Author,
            Content = entity.Content?.StripHTML()?.TextEllipsis(250),
            Tags = entity.Tags,

            ///<see cref="MetaValueMapping.ToDto"/>
            ///<see href="Mars\Mars.Host.Repositories\Mappings\MetaFieldMapping.cs"/>
            Meta = entity.MetaValues.ToDictionary(s => s.MetaField!.Key, v => ConvertObjectValue(v, fillDict)),
            Status = entity.Status,
        };

    public static IReadOnlyCollection<PostJsonDto> ToJsonDtoList(this IEnumerable<PostDetail> entities, MetaFieldRelatedFillDict? fillDict)
        => entities.Select(s => s.ToJsonDtoSummary(fillDict)).ToList();

    internal static object? ConvertObjectValue(MetaValueDto? metaValue, MetaFieldRelatedFillDict? fillDict)
    {
        if (metaValue == null) return null;

        if (metaValue.Value is MetaFieldVariantDto mvSel)
        {
            return mvSel.ToValueDto();
        }
        else if (metaValue.Value is MetaFieldVariantDto[] mvMany)
        {
            return mvMany.Select(s => s.ToValueDto()).ToArray();
        }
        else if (metaValue.Type is MetaFieldType.File or MetaFieldType.Image)
        {
            return fillDict?.GetValueOrDefault((metaValue.Type, null, metaValue.ModelId!.Value))?.ModelDto;
        }
        else if (metaValue.Type == MetaFieldType.Relation)
        {
            return fillDict?.GetValueOrDefault((metaValue.Type, metaValue.MetaField.ModelName, metaValue.ModelId!.Value))?.ModelDto;
        }

        return metaValue.Value;
    }

    public static PostJsonResponse ToResponse(this PostJsonDto entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Slug = entity.Slug,
            Title = entity.Title,
            Type = entity.Type,
            Content = entity.Content,
            Author = entity.Author.ToResponse(),
            Meta = entity.Meta,
        };

    public static ListDataResult<PostJsonResponse> ToResponse(this ListDataResult<PostJsonDto> list)
        => list.ToMap(ToResponse);

    public static PagingResult<PostJsonResponse> ToResponse(this PagingResult<PostJsonDto> page)
        => page.ToMap(ToResponse);
}
