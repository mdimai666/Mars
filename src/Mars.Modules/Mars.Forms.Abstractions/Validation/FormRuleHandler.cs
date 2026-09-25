using System.Text.Json.Nodes;
using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions.Validation;

/// <summary>
/// Обработчик правила валидации значения поля формы. Сигнатура повторяет
/// <c>MetaFieldValueValidators.Validator</c> — правила метаполей подключаются к общему
/// реестру без изменения кода.
/// </summary>
public delegate ValueTask<IEnumerable<string>> FormRuleHandler(object? value, JsonObject? parameters,
                                                               FormFieldValidationContext context,
                                                               CancellationToken cancellationToken);

/// <summary>Контекст проверки одного поля; <see cref="Field"/> и данные владельца заполняет валидатор</summary>
public sealed record FormFieldValidationContext
{
    public required string OwnerModel { get; init; }

    /// <summary>Ид сохраняемой записи (при создании — пусто)</summary>
    public string? OwnerId { get; init; }

    public FormFieldDescriptor? Field { get; init; }

    /// <summary>Сервисы провайдера — для правил, которым нужны данные владельца (<c>unique</c>)</summary>
    public IServiceProvider? Services { get; init; }
}

/// <summary>Контекст проверки всей формы</summary>
public sealed record FormValidationContext
{
    public string? OwnerId { get; init; }

    /// <summary>Сервисы провайдера для правил, обращающихся к данным</summary>
    public IServiceProvider? Services { get; init; }
}
