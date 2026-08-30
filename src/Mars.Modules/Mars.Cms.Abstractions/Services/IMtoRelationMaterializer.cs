using System.Collections;

namespace Mars.Cms.Abstractions.Services;

/// <summary>
/// Батч-материализация Relation-навигаций в скомпилированных Mto-моделях
/// (в сгенерированном селекте заполняется только {ключ}Id)
/// </summary>
public interface IMtoRelationMaterializer
{
    /// <summary>
    /// Дозаполняет Relation-свойства объектов Mto-типа поста по их {ключ}Id
    /// </summary>
    Task FillAsync(string typeName, IEnumerable items, CancellationToken cancellationToken);
}
