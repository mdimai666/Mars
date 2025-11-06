using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mars.Options.Attributes;
using Mars.Shared.Options.Attributes;
using Microsoft.AspNetCore.Components;

namespace AppFront.Main.OptionEditForms;

public class OptionsFormsLocator
{
    Dictionary<Type, OptionsFormsLocatorItem> _dict = [];
    public IReadOnlyDictionary<Type, OptionsFormsLocatorItem> Dict { get { if (invalid) RefreshDict(); return _dict; } }

    bool invalid = true;

    HashSet<Assembly> assemblies = [];

    private void RefreshDict(bool force = false)
    {
        if (!invalid && !force) return;
        _dict.Clear();

        foreach (var assembly in assemblies)
        {
            var types = GetOptionsEditForms(assembly);

            foreach (var a in types)
            {
                _dict.Add(a.Key, a.Value);
            }
        }
        invalid = false;
    }

    public void RegisterAssembly(Assembly assembly)
    {
        invalid = true;
        assemblies.Add(assembly);
    }

    public OptionsFormsLocatorItem? TryGetForOptionType(Type optionType)
    {
        return Dict.GetValueOrDefault(optionType);
    }

    public IEnumerable<OptionsFormsLocatorItem> RegisteredForms()
    {
        return Dict.Values.ToList();
    }

    public IEnumerable<OptionsFormsLocatorItem> RegisteredFormsAutoShow()
    {
        return Dict.Values.Where(s => s.IsAutoShowFormOnSettingsPageAttribute).ToList();
    }

    /// <summary>
    /// GetOptionsEditForms
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>Key NodeType; Valye FormType</returns>
    Dictionary<Type, OptionsFormsLocatorItem> GetOptionsEditForms(Assembly assembly)
    {
        var type = typeof(ComponentBase);

        var types =
            //AppDomain.CurrentDomain.GetAssemblies()
            //.SelectMany(s => s.GetTypes())
            //Assembly.GetAssembly(typeof(Program)).GetTypes()
            //Assembly.GetAssembly(program).GetTypes()
            assembly.GetTypes()
            .Where(p =>
                type.IsAssignableFrom(p)
                && p.IsPublic
                && p.IsClass
                && !p.IsAbstract
            //&& p.Assembly ==
            );

        var dict = new Dictionary<Type, OptionsFormsLocatorItem>();

        foreach (var formType in types)
        {
            OptionEditFormForOptionAttribute? attribute = formType.GetCustomAttribute<OptionEditFormForOptionAttribute>();

            if (attribute != null)
            {
                Type optionType = attribute.ForOptionType;

                var item = new OptionsFormsLocatorItem
                {
                    attribute1 = attribute,
                    FormType = formType,
                    OptionType = optionType,
                    IsAutoShowFormOnSettingsPageAttribute = formType.GetCustomAttribute<AutoShowFormOnSettingsPageAttribute>() != null,
                    DisplayName = formType.GetCustomAttribute<DisplayAttribute>()?.Name ?? formType.Name,
                };
                dict.Add(optionType, item);
            }

        }

        return dict;
    }

}

public record OptionsFormsLocatorItem
{
    public required Type FormType;
    public required Type OptionType;
    public required OptionEditFormForOptionAttribute attribute1;
    public bool IsAutoShowFormOnSettingsPageAttribute;
    public required string DisplayName;
}
