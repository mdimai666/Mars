using Mars.Forms.Abstractions.Validation;
using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions;

/// <summary>
/// Проверка мешка значений по определению формы: обязательность, форма значения
/// (<see cref="FormValueCodec"/>), пределы из дескриптора и правила (дескриптора + раскладки).
/// Правила метаполей сюда не попадают — их применяет пайплайн владельца поля.
/// </summary>
public interface IFormValidator
{
    Task<IReadOnlyCollection<FormError>> ValidateAsync(FormDefinition definition, FormValues values,
                                                       FormValidationContext? context = null,
                                                       CancellationToken cancellationToken = default);
}
