using System.Collections;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Services;
using Mars.Forms.Abstractions;
using Mars.Forms.Abstractions.Validation;
using Mars.Forms.Contracts;
using static Mars.Cms.Contracts.PostTypes.SystemFieldsCatalog;

namespace Mars.Cms.Abstractions.Forms;

/// <summary>
/// Правила раскладки формы для системных полей поста, применённые на сервере. Транспорт записи
/// не меняется (решение B(ii) плана): значения берутся из <see cref="IGeneralPostQuery"/> и
/// проверяются общим валидатором формы, поэтому правила действуют на всех путях записи —
/// и админ-форма, и JSON-API проходят валидаторы <c>CreatePostQuery</c>/<c>UpdatePostQuery</c>.
/// Метаполя проверяет <c>MetaValuesValidator</c>, контент — свой пайплайн; обязательность и
/// пределы слотов из дескриптора здесь сняты — их пол это DataAnnotations транспорта и правила
/// <see cref="GeneralPostQueryValidator"/>, а правила раскладки добавляются сверху.
/// </summary>
public class PostFormRulesValidator(IMetaModelTypesLocator metaModelTypesLocator,
                                    IFormDefinitionNormalizer formNormalizer,
                                    IFormValidator formValidator,
                                    IServiceProvider services)
{
    /// <summary>Слот формы → свойство транспорта записи. Слоты без свойства (даты) запрос не несёт</summary>
    static readonly Dictionary<string, string> TransportProperties = new(StringComparer.Ordinal)
    {
        [Title] = nameof(CreatePostQuery.Title),
        [Slug] = nameof(CreatePostQuery.Slug),
        [Excerpt] = nameof(CreatePostQuery.Excerpt),
        [Status] = nameof(CreatePostQuery.Status),
        [Lang] = nameof(CreatePostQuery.LangCode),
        [Author] = nameof(CreatePostQuery.UserId),
        [Categories] = nameof(CreatePostQuery.CategoryIds),
        [Tags] = nameof(CreatePostQuery.Tags),
    };

    /// <summary>Свойство запроса, куда пишется значение слота — имя поля в сообщении об ошибке</summary>
    public static string? TransportProperty(string slotKey) => TransportProperties.GetValueOrDefault(slotKey);

    /// <summary>Ид сохраняемого поста: у обновления — свой, у создания — заранее заданный (обычно пусто)</summary>
    public static Guid? OwnerId(IGeneralPostQuery query) => query switch
    {
        UpdatePostQuery update => update.Id,
        CreatePostQuery create => create.Id,
        _ => null,
    };

    public async Task<IReadOnlyCollection<FormError>> ValidateAsync(IGeneralPostQuery query, Guid? id,
                                                                    CancellationToken cancellationToken = default)
    {
        // отсутствие и отключённость типа проверяет основное правило валидатора запроса
        var postType = metaModelTypesLocator.GetPostTypeByName(query.Type);
        if (postType is null) return [];

        var definition = RulesOnly(PostFormBuilder.Build(postType, formNormalizer));
        if (definition.Items.Count == 0) return [];

        return await formValidator.ValidateAsync(definition, FormValuesOf(definition, query),
            new FormValidationContext { OwnerId = id?.ToString("D"), Services = services }, cancellationToken);
    }

    /// <summary>
    /// Определение только под правила раскладки: системные слоты, которые несёт транспорт записи
    /// и у которых есть правила, без дескрипторных обязательности и пределов. Видимость элемента
    /// не учитывается — правило защищает данные, а не разметку.
    /// </summary>
    public static FormDefinition RulesOnly(FormDefinition definition) => definition with
    {
        Items = definition.Fields()
                          .Where(item => item.Field is { SettingsOnForm: true } && TransportProperties.ContainsKey(item.Field.Key))
                          .Where(item => item.Rules.Count > 0 || item.Field!.Rules.Count > 0)
                          .Select(item => item with
                          {
                              Zone = null,
                              Items = [],
                              Field = item.Field with { Required = false, Min = null, Max = null },
                          })
                          .ToList(),
    };

    static FormValues FormValuesOf(FormDefinition definition, IGeneralPostQuery query)
    {
        var values = new FormValues { OwnerModel = definition.OwnerModel };

        foreach (var field in definition.Fields().Select(item => item.Field!))
        {
            var value = TransportValue(field.Key, query);
            if (value is null) continue;

            values.Values[field.Key] = field.Multiple
                ? FormValueCodec.FromClrList(value as IEnumerable, field.Type)
                : FormValueCodec.FromClr(value, field.Type);
        }

        return values;
    }

    static object? TransportValue(string key, IGeneralPostQuery query) => key switch
    {
        Title => query.Title,
        Slug => query.Slug,
        Excerpt => query.Excerpt,
        Status => query.Status,
        Lang => query.LangCode,
        Author => query.UserId,
        Categories => query.CategoryIds,
        Tags => query.Tags,
        _ => null,
    };
}
