using Mars.XActions.Contracts;

namespace Mars.Admin.Framework.Services;

/// <summary>
/// Показывает форму заполнения аргументов XAction и возвращает введённые значения
/// (null — пользователь отменил вызов). Реализация живёт в хосте (диалоги FluentUI
/// в админке); если презентер не зарегистрирован, команда вызывается без формы.
/// </summary>
public interface IXActionFormPresenter
{
    Task<IReadOnlyDictionary<string, string>?> ShowFormAsync(XActionCommand command);
}

internal class NullXActionFormPresenter : IXActionFormPresenter
{
    public Task<IReadOnlyDictionary<string, string>?> ShowFormAsync(XActionCommand command)
        => Task.FromResult<IReadOnlyDictionary<string, string>?>(null);
}

/// <summary>
/// Реестр кастомных форм аргументов: actionId → Blazor-компонент диалога
/// (реализует IDialogContentComponent&lt;XActionCommand&gt; и возвращает словарь значений).
/// Кастомная форма перекрывает генерик-форму по схеме.
/// </summary>
public interface IXActionFormProvider
{
    void Register(string actionId, Type dialogComponentType);
    Type? GetForm(string actionId);
}

public class XActionFormProvider : IXActionFormProvider
{
    readonly Dictionary<string, Type> _forms = [];

    public void Register(string actionId, Type dialogComponentType)
        => _forms[actionId] = dialogComponentType;

    public Type? GetForm(string actionId)
        => _forms.GetValueOrDefault(actionId);
}
