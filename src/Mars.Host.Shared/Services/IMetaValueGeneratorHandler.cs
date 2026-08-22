using System.Text.Json.Nodes;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostTypes;

namespace Mars.Host.Shared.Services;

/// <summary>
/// Контекст генерации значения: создаваемый объект и поле, для которого генерируется значение.
/// </summary>
/// <param name="PostType">Тип создаваемого поста</param>
/// <param name="Field">Поле, для которого генерируется значение</param>
/// <param name="CategorySlugs">Slug'ы категорий создаваемого поста (в порядке привязки)</param>
/// <param name="Now">Момент создания</param>
public record MetaValueGeneratorContext(
    PostTypeDetail PostType,
    MetaFieldDto Field,
    IReadOnlyList<string> CategorySlugs,
    DateTimeOffset Now);

/// <summary>
/// Генератор значения мета-поля. Реализации регистрируются как keyed-сервисы
/// с ключом из <c>Mars.Shared.Contracts.MetaFields.MetaFieldGeneratorCatalog</c>
/// (паттерн целей Relation — <c>IMetaRelationModelProviderHandler</c>).
/// </summary>
public interface IMetaValueGeneratorHandler
{
    /// <returns>Значение в типе поля (строка для String, DateTime для DateTime и т.д.)</returns>
    Task<object?> GenerateAsync(MetaValueGeneratorContext context, JsonObject? parameters, CancellationToken cancellationToken);
}
