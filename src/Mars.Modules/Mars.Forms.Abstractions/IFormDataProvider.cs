using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions;

/// <summary>
/// Провайдер формы: отдаёт определение (дерево с дескрипторами и зонами). Регистрация — keyed-DI
/// по <see cref="OwnerModel"/> (паттерн <c>IMetaRelationModelProviderHandler</c>): точный ключ
/// (<c>form.feedback</c>) или шаблон (<c>post.*</c>). Значения провайдер не читает и не принимает:
/// транспорт записи остаётся у владельца (форма поста — <c>PostEditModel</c> → <c>PostRequest</c>).
/// </summary>
public interface IFormDataProvider
{
    /// <summary>Модель-владелец: точный ключ или шаблон <c>prefix.*</c></summary>
    string OwnerModel { get; }

    /// <summary>Нормализованное определение формы: дерево с дескрипторами и зонами</summary>
    Task<FormDefinition> GetFormAsync(FormContext context, CancellationToken cancellationToken);
}

/// <summary>Поиск провайдера по модели владельца (точный ключ, затем шаблоны <c>prefix.*</c>)</summary>
public interface IFormDataProviderLocator
{
    IFormDataProvider? GetProvider(string ownerModel, IServiceProvider serviceProvider);

    /// <summary>Зарегистрированные ключи провайдеров (для списков и диагностики)</summary>
    IReadOnlyCollection<string> OwnerModels { get; }
}
