using Mars.Forms.Abstractions.Validation;
using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions;

/// <summary>
/// Реестр правил валидации значений формы. Реализации глобальные, но скоупятся по модели
/// владельца: поиск идёт «точный ключ → шаблоны <c>prefix.*</c> → глобальные», поэтому
/// у провайдеров свои наборы правил. Неизвестное правило мягко пропускается
/// (как в реестре валидаторов метаполей).
/// </summary>
public interface IFormRuleRegistry
{
    /// <summary>Глобальная регистрация/замена правила</summary>
    void Register(string type, FormRuleHandler handler);

    /// <summary>Регистрация правила в скоупе провайдера (<c>post.*</c>, <c>sql.ds1.*</c>, точный ключ)</summary>
    void Register(string ownerModel, string type, FormRuleHandler handler);

    bool IsKnown(string ownerModel, string type);

    /// <summary>Известные правила для модели владельца — их показывает дизайнер формы</summary>
    IReadOnlyCollection<string> KnownTypes(string ownerModel);

    ValueTask<IEnumerable<string>> ValidateAsync(FormRuleDefinition rule, object? value,
                                                 FormFieldValidationContext context, CancellationToken cancellationToken);
}
