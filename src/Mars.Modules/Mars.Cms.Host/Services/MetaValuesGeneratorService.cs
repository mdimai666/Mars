using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Core.Exceptions;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Services;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Abstractions.Dto.PostCategories;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Services;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Services;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Services;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Abstractions.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Cms.Host.Services;

internal class MetaValuesGeneratorService : IMetaValuesGeneratorService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPostCategoryRepository _postCategoryRepository;
    private readonly IMetaValuesValidator _metaValuesValidator;
    private readonly IPostRepository _postRepository;
    private readonly IMetaSequenceRepository _metaSequenceRepository;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;

    public MetaValuesGeneratorService(
        IServiceProvider serviceProvider,
        IPostCategoryRepository postCategoryRepository,
        IMetaValuesValidator metaValuesValidator,
        IPostRepository postRepository,
        IMetaSequenceRepository metaSequenceRepository,
        IMetaModelTypesLocator metaModelTypesLocator)
    {
        _serviceProvider = serviceProvider;
        _postCategoryRepository = postCategoryRepository;
        _metaValuesValidator = metaValuesValidator;
        _postRepository = postRepository;
        _metaSequenceRepository = metaSequenceRepository;
        _metaModelTypesLocator = metaModelTypesLocator;
    }

    public async Task<CreatePostQuery> ApplyAsync(PostTypeDetail postType, CreatePostQuery query, CancellationToken cancellationToken)
    {
        var contentFieldKey = postType.ContentField()?.Key;
        var generatorFields = postType.MetaFields
            .Select(f => (Field: f, Generator: MetaFieldGeneratorDefinition.FromOptions(f.Options)))
            .Where(x => x.Generator is not null && x.Field.Type != MetaFieldType.Query && x.Field.Key != contentFieldKey)
            .ToList();

        if (generatorFields.Count == 0) return query;

        var values = query.MetaValues.ToList();
        var generated = new List<ModifyMetaValueDetailQuery>();
        IReadOnlyList<string>? categorySlugs = null;

        foreach (var (field, generator) in generatorFields)
        {
            // явно заданное значение не перезаписывается
            var existing = values.Where(v => v.MetaFieldId == field.Id).OrderBy(v => v.Index).FirstOrDefault();
            if (existing is not null && !IsEmptyValue(existing.GetValueSimple())) continue;

            var handler = _serviceProvider.GetKeyedService<IMetaValueGeneratorHandler>(generator!.Type)
                ?? throw MarsValidationException.FromSingleError("generator",
                    $"unknown generator '{generator.Type}' for field '{field.Key}'");

            categorySlugs ??= await LoadCategorySlugsAsync(query.CategoryIds, cancellationToken);

            var context = new MetaValueGeneratorContext(postType, field, categorySlugs, DateTimeOffset.Now);
            var value = await handler.GenerateAsync(context, generator.Params, cancellationToken);
            if (value is null) continue;

            var newValue = MetaFieldUtils.MetaValueFromObject(existing ?? ModifyMetaValueDetailQuery.GetBlank(field), value);

            if (existing is not null) values[values.IndexOf(existing)] = newValue;
            else values.Add(newValue);

            generated.Add(newValue);
        }

        if (generated.Count == 0) return query;

        var errors = await _metaValuesValidator.ValidateAsync(generated,
            new MetaValueValidationContext { ModelName = MetaValueOwnerCatalog.Post }, cancellationToken);
        if (errors.Count > 0)
            throw new MarsValidationException(errors.GroupBy(e => e.FieldKey)
                                                    .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray()));

        return query with { MetaValues = values };
    }

    static bool IsEmptyValue(object? value) => value is null || (value is string text && text.Length == 0);

    public async Task<RegenerateMetaValuesResult> RegenerateAsync(RegenerateMetaValuesQuery query, CancellationToken cancellationToken)
    {
        var postType = _metaModelTypesLocator.GetPostTypeByName(query.PostTypeName)
            ?? throw new NotFoundException($"post type '{query.PostTypeName}' not found");

        var contentFieldKey = postType.ContentField()?.Key;
        var generatorFields = postType.MetaFields
            .Select(f => (Field: f, Generator: MetaFieldGeneratorDefinition.FromOptions(f.Options)))
            .Where(x => x.Generator is not null && x.Field.Type != MetaFieldType.Query && x.Field.Key != contentFieldKey)
            .ToList();

        if (generatorFields.Count == 0) return new RegenerateMetaValuesResult(0, 0);

        var posts = (await _postRepository.ListAllDetail(new ListAllPostQuery { Type = query.PostTypeName }, cancellationToken)).ToList();

        if (query.StatusSlugs is { Count: > 0 } statuses)
            posts = posts.Where(p => p.Status is not null && statuses.Contains(p.Status.Value.Key)).ToList();

        if (query.Mode == MetaValuesRegenerationMode.Today)
        {
            var today = DateTimeOffset.Now.Date;
            posts = posts.Where(p => p.CreatedAt.Date == today).ToList();
        }

        posts = posts.OrderBy(p => p.CreatedAt).ToList();
        if (posts.Count == 0) return new RegenerateMetaValuesResult(0, 0);

        var upserts = new List<PostMetaValueUpsert>();
        var counterSets = new List<(Guid FieldId, string Scope, long Value)>();

        foreach (var (field, generator) in generatorFields)
        {
            if (generator!.Type == MetaFieldGeneratorCatalog.Sequence)
            {
                await RegenerateSequenceAsync(field, generator, posts, query.Mode, upserts, counterSets, cancellationToken);
            }
            else if (generator.Type == MetaFieldGeneratorCatalog.Now)
            {
                // дозаполнение пустых дат значением момента создания поста
                foreach (var post in posts)
                {
                    if (!IsEmptyMeta(post, field.Key)) continue;
                    upserts.Add(new PostMetaValueUpsert(post.Id, field, post.CreatedAt.DateTime));
                }
            }
        }

        foreach (var (fieldId, scope, value) in counterSets)
            await _metaSequenceRepository.SetValueAsync(fieldId, scope, value, cancellationToken);

        if (upserts.Count > 0)
            await _postRepository.UpsertMetaValuesAsync(upserts, cancellationToken);

        return new RegenerateMetaValuesResult(posts.Count, upserts.Count);
    }

    async Task RegenerateSequenceAsync(MetaFieldDto field,
                                       MetaFieldGeneratorDefinition generator,
                                       List<PostDetail> posts,
                                       MetaValuesRegenerationMode mode,
                                       List<PostMetaValueUpsert> upserts,
                                       List<(Guid FieldId, string Scope, long Value)> counterSets,
                                       CancellationToken cancellationToken)
    {
        var daily = SequenceValueGeneratorHandler.IsDaily(generator.Params);

        if (mode == MetaValuesRegenerationMode.FromLast)
        {
            // существующие значения не трогаем — пустые дозаполняются продолжением счётчика
            foreach (var post in posts)
            {
                if (!IsEmptyMeta(post, field.Key)) continue;

                var prefix = SequenceValueGeneratorHandler.ResolvePrefix(generator.Params, CategorySlugs(post));
                var scope = SequenceValueGeneratorHandler.ScopeKey(prefix, daily, post.CreatedAt);
                var number = await _metaSequenceRepository.NextValueAsync(field.Id, scope, cancellationToken);

                upserts.Add(new PostMetaValueUpsert(post.Id, field, SequenceValueGeneratorHandler.Format(prefix, number, generator.Params)));
            }
            return;
        }

        // перенумерация с 1 по скоупам (префикс, при ежедневном режиме — + дата создания поста)
        var counters = new Dictionary<string, long>();
        foreach (var post in posts)
        {
            var prefix = SequenceValueGeneratorHandler.ResolvePrefix(generator.Params, CategorySlugs(post));
            var scope = SequenceValueGeneratorHandler.ScopeKey(prefix, daily, post.CreatedAt);

            var number = counters.GetValueOrDefault(scope) + 1;
            counters[scope] = number;

            upserts.Add(new PostMetaValueUpsert(post.Id, field, SequenceValueGeneratorHandler.Format(prefix, number, generator.Params)));
        }

        foreach (var (scope, lastNumber) in counters)
            counterSets.Add((field.Id, scope, lastNumber));
    }

    static IReadOnlyList<string> CategorySlugs(PostDetail post)
        => post.Categories?.Select(c => c.Slug).ToList() ?? [];

    static bool IsEmptyMeta(PostDetail post, string fieldKey)
        => !post.MetaValues.TryGetValue(fieldKey, out var value) || IsEmptyValue(value.Value);

    /// <summary>Slug'ы категорий поста в порядке привязки (для словаря категория→префикс)</summary>
    async Task<IReadOnlyList<string>> LoadCategorySlugsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0) return [];

        var categories = await _postCategoryRepository.ListAll(
            new ListAllPostCategoryQuery { Type = null, Ids = categoryIds, PostTypeName = null }, cancellationToken);

        var slugsById = categories.ToDictionary(c => c.Id, c => c.Slug);
        return categoryIds.Where(slugsById.ContainsKey)
                          .Select(id => slugsById[id])
                          .ToList();
    }
}
