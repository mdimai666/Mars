using System.Text.Json.Nodes;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Forms;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Abstractions;
using Mars.Forms.Contracts;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.Server.Tests.Forms;

/// <summary>
/// Общий фикстур формы поста: настоящие нормализатор и валидатор общего слоя, подставные данные
/// владельца, построители типа поста и метаполя.
/// </summary>
public static class PostFormTestHost
{
    public static readonly IFormDefinitionNormalizer Normalizer =
        new ServiceCollection().AddMarsForms().BuildServiceProvider().GetRequiredService<IFormDefinitionNormalizer>();

    public static readonly string[] AllFeatures =
    [
        PostTypeConstants.Features.Content,
        PostTypeConstants.Features.Status,
        PostTypeConstants.Features.ModifyCreatedDate,
        PostTypeConstants.Features.Language,
        PostTypeConstants.Features.Tags,
        PostTypeConstants.Features.Excerpt,
        PostTypeConstants.Features.Category,
    ];

    public static PostFormRulesValidator RulesValidator(IMetaModelTypesLocator locator, IPostRepository? posts = null)
    {
        var services = new ServiceCollection()
            .AddMarsForms()
            .AddSingleton<IFormRulesContributor, PostFormRulesContributor>()
            .AddSingleton(locator)
            .AddSingleton(posts ?? Substitute.For<IPostRepository>())
            .BuildServiceProvider();

        return new PostFormRulesValidator(locator,
            services.GetRequiredService<IFormDefinitionNormalizer>(),
            services.GetRequiredService<IFormValidator>(),
            services);
    }

    public static PostFormRulesValidator RulesValidator(PostTypeDetail postType, out IPostRepository posts)
    {
        var locator = Substitute.For<IMetaModelTypesLocator>();
        locator.GetPostTypeByName(postType.TypeName).Returns(postType);

        posts = Substitute.For<IPostRepository>();
        return RulesValidator(locator, posts);
    }

    //=====================================

    public static PostTypeDetail Type(IReadOnlyCollection<string> features,
                                      params MetaFieldDto[] metaFields)
        => Type(features, [], metaFields);

    public static PostTypeDetail Type(IReadOnlyCollection<string> features,
                                      IReadOnlyCollection<PostStatusDto> statuses,
                                      IReadOnlyCollection<MetaFieldDto>? metaFields = null) => new()
    {
        Id = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.Now,
        ModifiedAt = null,
        Title = "Статья",
        TypeName = "article",
        Tags = [],
        EnabledFeatures = features,
        Disabled = false,
        Visibility = PostTypeVisibility.Public,
        PostStatusList = statuses,
        MetaFields = metaFields ?? [],
        Presentation = PostTypePresentation.Default(),
    };

    public static PostStatusDto Status(string slug, string title) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Title = title,
        Color = "",
        Order = 0,
    };

    /// <summary>Поле контента, которое создаёт фича Content</summary>
    public static MetaFieldDto Content() => Meta(FeatureFieldsCatalog.ContentFieldKey, 0,
        type: MetaFieldType.Text, featureKey: FeatureFieldsCatalog.Content);

    public static MetaFieldDto Meta(string key, int order, MetaFieldType type = MetaFieldType.String,
                                    bool hidden = false, bool disabled = false, string? featureKey = null) => new()
    {
        Id = Guid.NewGuid(),
        Title = key,
        Key = key,
        Type = type,
        MaxValue = null,
        MinValue = null,
        Description = "",
        IsNullable = true,
        IsMultiple = false,
        Default = null,
        Options = featureKey is null ? null : new JsonObject { [FeatureFieldsCatalog.FeatureKeyOption()] = featureKey },
        Order = order,
        Tags = [],
        Hidden = hidden,
        Disabled = disabled,
        Variants = null,
        ModelName = null,
    };

    /// <summary>Сохранённая раскладка: элементы в порядке аргументов</summary>
    public static FormLayoutSettings Layout(params FormItem[] items) => new() { Items = items };

    /// <summary>Элемент раскладки системного слота с правилами</summary>
    public static FormItem Slot(string key, params FormRuleDefinition[] rules) => new()
    {
        Key = key,
        Rules = rules,
    };

    public static FormRuleDefinition Rule(string type, JsonObject? parameters = null)
        => new() { Type = type, Params = parameters };
}
