using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.PostJsons;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Mappings.MetaFields;
using Mars.Cms.Abstractions.Mappings.PostCategories;
using Mars.Cms.Abstractions.Mappings.PostJsons;
using Mars.Cms.Abstractions.Mappings.Posts;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostJsons;
using Mars.Contracts.Common;
using Mars.Contracts.Extensions;
using Mars.Core.Extensions;

namespace Mars.Cms.Abstractions.Mappings.PostJsons;

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
            ///<see href="Mars\Mars.Data.Repositories\Mappings\MetaFieldMapping.cs"/>
            Meta = entity.MetaValues.ToDictionary(s => s!.Key, v => ConvertMetaValue(v.Value, fillDict)),
            Status = entity.Status,
            Categories = entity.Categories,
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
            ///<see href="Mars\Mars.Data.Repositories\Mappings\MetaFieldMapping.cs"/>
            Meta = entity.MetaValues.ToDictionary(s => s!.Key, v => ConvertMetaValue(v.Value, fillDict)),
            Status = entity.Status,
            Categories = entity.Categories,
        };

    public static IReadOnlyCollection<PostJsonDto> ToJsonDtoList(this IEnumerable<PostDetail> entities, MetaFieldRelatedFillDict? fillDict)
        => entities.Select(s => s.ToJsonDtoSummary(fillDict)).ToList();

    /// <summary>
    /// Мульти-значения поля (несколько строк) отдаёт массивом
    /// </summary>
    internal static object? ConvertMetaValue(MetaValueDto? metaValue, MetaFieldRelatedFillDict? fillDict)
    {
        if (metaValue?.MultiValues is not null)
        {
            return metaValue.MultiValues.Select(s => ConvertObjectValue(s, fillDict)).ToArray();
        }
        return ConvertObjectValue(metaValue, fillDict);
    }

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
            Tags = entity.Tags,
            Content = entity.Content,
            Author = entity.Author.ToResponse(),
            Meta = entity.Meta,
            Categories = entity.Categories?.ToResponse(),
        };

    public static ListDataResult<PostJsonResponse> ToResponse(this ListDataResult<PostJsonDto> list)
        => list.ToMap(ToResponse);

    public static PagingResult<PostJsonResponse> ToResponse(this PagingResult<PostJsonDto> page)
        => page.ToMap(ToResponse);
}
