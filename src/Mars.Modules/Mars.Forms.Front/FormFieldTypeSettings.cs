using Mars.Forms.Contracts;

namespace Mars.Forms.Front;

/// <summary>
/// Реестр панелей доменных настроек поля для общего редактора определений:
/// скоуп источника + тип поля → компонент панели. Панель принимает <see cref="FormFieldDefinition"/>
/// и правит доменную часть (цель связи, папка загрузки, варианты, генератор…); общие параметры
/// (заголовок, ключ, обязательность, правила, редактор) правит сам редактор — панели их не дублируют.
/// </summary>
public interface IFormFieldTypeSettingsLocator
{
    /// <summary>Панели настроек для поля: общие панели скоупа + панели типа</summary>
    IReadOnlyCollection<Type> PanelsFor(string scope, FormFieldType fieldType);
}

/// <summary>
/// Регистрация панели доменных настроек поля. Без типов — панель показывается на всех полях скоупа
/// (сама решает, что рисовать). Один и тот же компонент в скоупе регистрируется один раз.
/// </summary>
public sealed record FormFieldTypeSettingsRegistration(string Scope, Type Component,
                                                       IReadOnlyCollection<FormFieldType> FieldTypes);

public sealed class FormFieldTypeSettingsLocator : IFormFieldTypeSettingsLocator
{
    /// <summary>Ключ типа для панелей, которые показываются на всех полях скоупа</summary>
    public const string AnyType = "*";

    readonly Dictionary<(string Scope, string Type), List<Type>> _registry;

    public FormFieldTypeSettingsLocator(IEnumerable<FormFieldTypeSettingsRegistration>? registrations = null)
    {
        _registry = [];

        foreach (var registration in registrations ?? [])
        {
            if (registration.FieldTypes.Count == 0)
            {
                Add(registration.Scope, AnyType, registration.Component);
                continue;
            }

            foreach (var fieldType in registration.FieldTypes)
                Add(registration.Scope, fieldType.ToString(), registration.Component);
        }
    }

    public IReadOnlyCollection<Type> PanelsFor(string scope, FormFieldType fieldType)
    {
        var panels = new List<Type>();

        if (_registry.TryGetValue((scope, AnyType), out var any)) panels.AddRange(any);
        if (_registry.TryGetValue((scope, fieldType.ToString()), out var typed)) panels.AddRange(typed);

        return panels.Distinct().ToList();
    }

    void Add(string scope, string typeKey, Type component)
    {
        if (!_registry.TryGetValue((scope, typeKey), out var panels))
            _registry[(scope, typeKey)] = panels = [];

        if (!panels.Contains(component)) panels.Add(component);
    }
}
