using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions;

/// <summary>
/// Провайдер формы: отдаёт определение (дескрипторы + дерево) и, если умеет, читает и принимает
/// значения. Регистрация — keyed-DI по <see cref="OwnerModel"/> (паттерн
/// <c>IMetaRelationModelProviderHandler</c>): точный ключ (<c>form.feedback</c>) или шаблон (<c>post.*</c>).
/// Провайдер, у которого значения ходят своим типизированным транспортом (форма поста),
/// реализует только <see cref="GetFormAsync"/> и объявляет это в манифесте
/// (<see cref="FormProviderCapabilities.CanReadValues"/> / <see cref="FormProviderCapabilities.CanSubmit"/>).
/// </summary>
public interface IFormDataProvider
{
    /// <summary>Модель-владелец: точный ключ или шаблон <c>prefix.*</c></summary>
    string OwnerModel { get; }

    /// <summary>Нормализованное определение формы: дерево с дескрипторами и манифестом</summary>
    Task<FormDefinition> GetFormAsync(FormContext context, CancellationToken cancellationToken);

    /// <summary>Значения формы в канонических кодировках (<see cref="FormValueCodec"/>)</summary>
    Task<FormValues> ReadAsync(FormContext context, CancellationToken cancellationToken)
        => throw new NotSupportedException($"провайдер формы '{OwnerModel}' не читает значения");

    /// <summary>Валидация правилами определения и запись в хранилище провайдера</summary>
    Task<FormSubmitResult> SubmitAsync(FormContext context, FormValues values, CancellationToken cancellationToken)
        => throw new NotSupportedException($"провайдер формы '{OwnerModel}' не принимает значения");
}

/// <summary>Поиск провайдера по модели владельца (точный ключ, затем шаблоны <c>prefix.*</c>)</summary>
public interface IFormDataProviderLocator
{
    IFormDataProvider? GetProvider(string ownerModel, IServiceProvider serviceProvider);

    /// <summary>Зарегистрированные ключи провайдеров (для списков и диагностики)</summary>
    IReadOnlyCollection<string> OwnerModels { get; }
}
