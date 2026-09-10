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
    /// <summary>
    /// Регистрация панели. Без типов — панель показывается на всех полях скоупа (сама решает,
    /// что рисовать). Один и тот же компонент в скоупе регистрируется один раз.
    /// </summary>
    void Register(string scope, Type component, params FormFieldType[] fieldTypes);

    /// <summary>Панели настроек для поля: общие панели скоупа + панели типа</summary>
    IReadOnlyCollection<Type> PanelsFor(string scope, FormFieldType fieldType);
}

/// <summary>Запись реестра панелей настроек: скоуп → компонент + типы полей (пусто — все типы скоупа)</summary>
public sealed record FormFieldTypeSettingsRegistration(string Scope, Type Component,
                                                       IReadOnlyCollection<FormFieldType> FieldTypes);

/// <summary>
/// Экземпляр реестра панелей настроек (регистрируется синглтоном в DI): зарегистрировать панель
/// можно откуда угодно и когда угодно — записи складываются в список, а словарь по скоупу и типу
/// собирается в момент запроса (аналогично локатору редакторов).
/// </summary>
public sealed class FormFieldTypeSettingsLocator : IFormFieldTypeSettingsLocator
{
    /// <summary>Ключ типа для панелей, которые показываются на всех полях скоупа</summary>
    public const string AnyType = "*";

    readonly object _lock = new();
    readonly List<FormFieldTypeSettingsRegistration> _registrations = [];
    volatile Dictionary<(string Scope, string Type), List<Type>>? _registry;

    public void Register(string scope, Type component, params FormFieldType[] fieldTypes)
    {
        lock (_lock)
        {
            _registrations.Add(new FormFieldTypeSettingsRegistration(scope, component, fieldTypes));
            _registry = null;
        }
    }

    public IReadOnlyCollection<Type> PanelsFor(string scope, FormFieldType fieldType)
    {
        var registry = Registry;
        var panels = new List<Type>();

        if (registry.TryGetValue((scope, AnyType), out var any)) panels.AddRange(any);
        if (registry.TryGetValue((scope, fieldType.ToString()), out var typed)) panels.AddRange(typed);

        return panels.Distinct().ToList();
    }

    /// <summary>Словарь по скоупу и типу поля: собирается в новый экземпляр и подменяется атомарно</summary>
    Dictionary<(string Scope, string Type), List<Type>> Registry
    {
        get
        {
            var registry = _registry;
            if (registry is not null) return registry;

            lock (_lock)
            {
                if (_registry is not null) return _registry;

                var built = new Dictionary<(string Scope, string Type), List<Type>>();

                foreach (var registration in _registrations)
                {
                    if (registration.FieldTypes.Count == 0)
                    {
                        Add(built, registration.Scope, AnyType, registration.Component);
                        continue;
                    }

                    foreach (var fieldType in registration.FieldTypes)
                        Add(built, registration.Scope, fieldType.ToString(), registration.Component);
                }

                return _registry = built;
            }
        }
    }

    static void Add(Dictionary<(string Scope, string Type), List<Type>> registry, string scope, string typeKey,
                    Type component)
    {
        if (!registry.TryGetValue((scope, typeKey), out var panels))
            registry[(scope, typeKey)] = panels = [];

        if (!panels.Contains(component)) panels.Add(component);
    }
}
