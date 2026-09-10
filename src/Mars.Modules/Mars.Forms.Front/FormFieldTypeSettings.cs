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

public class FormFieldTypeSettingsLocator : IFormFieldTypeSettingsLocator
{
    /// <summary>Ключ типа для панелей, которые показываются на всех полях скоупа</summary>
    public const string AnyType = "*";

    static readonly Dictionary<(string Scope, string Type), List<Type>> Registry = new();

    static readonly object RegistrationLock = new();

    /// <summary>
    /// Регистрация панели (модуль, плагин, админка) — до рендеринга.
    /// Без типов — панель показывается на всех полях скоупа (сама решает, что рисовать).
    /// </summary>
    public static void Register(string scope, Type component, params FormFieldType[] fieldTypes)
    {
        lock (RegistrationLock)
        {
            if (fieldTypes.Length == 0) Add(scope, AnyType, component);
            else
                foreach (var fieldType in fieldTypes)
                    Add(scope, fieldType.ToString(), component);
        }
    }

    static void Add(string scope, string typeKey, Type component)
    {
        if (!Registry.TryGetValue((scope, typeKey), out var panels))
            Registry[(scope, typeKey)] = panels = [];

        if (!panels.Contains(component)) panels.Add(component);
    }

    public IReadOnlyCollection<Type> PanelsFor(string scope, FormFieldType fieldType)
    {
        lock (RegistrationLock)
        {
            var panels = new List<Type>();

            if (Registry.TryGetValue((scope, AnyType), out var any)) panels.AddRange(any);
            if (Registry.TryGetValue((scope, fieldType.ToString()), out var typed)) panels.AddRange(typed);

            return panels.Distinct().ToList();
        }
    }
}
