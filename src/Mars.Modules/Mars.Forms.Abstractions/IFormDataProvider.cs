using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions;

/// <summary>
/// Провайдер формы: отдаёт определение (дескрипторы + дерево), читает значения и принимает
/// универсальный мешок, который сам валидирует и раскладывает по своему хранилищу.
/// Регистрация — keyed-DI по <see cref="OwnerModel"/> (паттерн <c>IMetaRelationModelProviderHandler</c>).
/// </summary>
public interface IFormDataProvider
{
    /// <summary>Модель-владелец: точный ключ (<c>post.article</c>) или шаблон (<c>post.*</c>)</summary>
    string OwnerModel { get; }

    /// <summary>Нормализованное определение формы: дерево с дескрипторами и манифестом</summary>
    Task<FormDefinition> GetFormAsync(FormContext context, CancellationToken cancellationToken);

    /// <summary>Значения формы в канонических кодировках (<see cref="FormValueCodec"/>)</summary>
    Task<FormValues> ReadAsync(FormContext context, CancellationToken cancellationToken);

    /// <summary>Валидация правилами определения и запись в хранилище провайдера</summary>
    Task<FormSubmitResult> SubmitAsync(FormContext context, FormValues values, CancellationToken cancellationToken);
}

/// <summary>Поиск провайдера по модели владельца (точный ключ, затем шаблоны <c>prefix.*</c>)</summary>
public interface IFormDataProviderLocator
{
    IFormDataProvider? GetProvider(string ownerModel, IServiceProvider serviceProvider);

    /// <summary>Зарегистрированные ключи провайдеров (для списков и диагностики)</summary>
    IReadOnlyCollection<string> OwnerModels { get; }
}
