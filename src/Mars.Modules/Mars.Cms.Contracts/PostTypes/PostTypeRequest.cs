using System.ComponentModel.DataAnnotations;
using Mars.Cms.Contracts.MetaFields;
using Mars.Contracts.Common;

namespace Mars.Cms.Contracts.PostTypes;

public record CreatePostTypeRequest
{
    public required Guid Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [StringLength(1000, MinimumLength = 3)]
    [Required]
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<CreatePostStatusRequest> PostStatusList { get; init; }
    public required IReadOnlyCollection<string> EnabledFeatures { get; init; }
    public required bool Disabled { get; init; }
    public required PostTypeVisibility Visibility { get; init; }
    public string? ImageFieldKey { get; init; }

    public required IReadOnlyCollection<CreateMetaFieldRequest> MetaFields { get; init; }
}

public record UpdatePostTypeRequest
{
    public required Guid Id { get; init; }

    [Required]
    public required string Title { get; init; }

    [StringLength(1000, MinimumLength = 3)]
    [Required]
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required IReadOnlyCollection<UpdatePostStatusRequest> PostStatusList { get; init; }
    public required IReadOnlyCollection<string> EnabledFeatures { get; init; }
    public required bool Disabled { get; init; }
    public required PostTypeVisibility Visibility { get; init; }
    public string? ImageFieldKey { get; init; }

    public required IReadOnlyCollection<UpdateMetaFieldRequest> MetaFields { get; init; }
}

public record CreatePostStatusRequest
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string Color { get; init; }
    public required int Order { get; init; }
}

public record UpdatePostStatusRequest
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string Color { get; init; }
    public required int Order { get; init; }
}

public record ListPostTypeQueryRequest : BasicListQueryRequest
{
    /// <summary>Показывать встроенные типы-компоненты (по умолчанию скрыты)</summary>
    public bool IncludeComponent { get; init; }
}

public record TablePostTypeQueryRequest : BasicTableQueryRequest
{
    /// <summary>Показывать встроенные типы-компоненты (по умолчанию скрыты)</summary>
    public bool IncludeComponent { get; init; }
}

public record UpdatePostTypePresentationRequest
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Относительный путь к шаблону списка во фронте админки (data/admin/front),
    /// например postTypes/article/listView.hbs. Пусто — стандартный вывод.
    /// </summary>
    public required string ListViewTemplate { get; init; }

    /// <summary>Настройки грида постов в админке; null — стандартный набор колонок</summary>
    public PostTypeGridSettings? Grid { get; init; }
}
