using System.Text.Json.Nodes;
using Mars.Cms.Abstractions.Repositories;
using Mars.Forms.Abstractions;
using Mars.Forms.Abstractions.Validation;
using Mars.Forms.Contracts;
using Microsoft.Extensions.DependencyInjection;
using static Mars.Cms.Contracts.PostTypes.SystemFieldsCatalog;

namespace Mars.Cms.Abstractions.Forms;

/// <summary>
/// Правила формы поста, которым нужны данные владельца. Сейчас это <c>unique</c> для slug —
/// единственный системный слот с семантикой уникальности: индексы <c>posts.slug</c>
/// сравнивают по <c>lower()</c>, поэтому проверка идёт тем же сравнением. Правило включает
/// администратор типа в раскладке формы — по умолчанию уникальность slug не требуется
/// (существующие записи могли создаваться без неё).
/// </summary>
public static class PostFormRules
{
    public static void RegisterAll(IFormRuleRegistry registry)
        => registry.Register(PostFormBuilder.OwnerModelWildcard, FormRuleCatalog.Unique, Unique);

    static async ValueTask<IEnumerable<string>> Unique(object? value, JsonObject? parameters,
                                                       FormFieldValidationContext context,
                                                       CancellationToken cancellationToken)
    {
        if (context.Field?.Key != Slug) return [];
        if (value is not string slug || slug.Length == 0) return [];
        if (context.Services?.GetService<IPostRepository>() is not { } repository) return [];

        Guid.TryParse(context.OwnerId, out var ownId);

        var occupied = await repository.SlugOccupiedAsync(PostFormBuilder.PostTypeName(context.OwnerModel), slug,
            ownId == Guid.Empty ? null : ownId, cancellationToken);

        return occupied ? [Message(parameters)] : [];
    }

    static string Message(JsonObject? parameters)
        => parameters?["message"] is JsonValue value && value.TryGetValue<string>(out var text) && text.Length > 0
            ? text
            : "значение уже занято";
}

/// <summary>Взнос правил поста в общий реестр (см. <see cref="IFormRulesContributor"/>)</summary>
public class PostFormRulesContributor : IFormRulesContributor
{
    public void Register(IFormRuleRegistry registry) => PostFormRules.RegisterAll(registry);
}
