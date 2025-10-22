using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mars.Options.Attributes;
using Mars.Shared.Options.Attributes;
using Microsoft.AspNetCore.Components;

namespace AppFront.Main.OptionEditForms;

public static class OptionsFormsLocator
{
    static Dictionary<Type, OptionsFormsLocatorItem> dict = new();

    static bool invalid = true;

    static List<Assembly> assemblies = new();

    static void RefreshDict(bool force = false)
    {
        if (!invalid && !force) return;
        dict.Clear();

        foreach (var assembly in assemblies)
        {
            var _dict = GetOptionsEditForms(assembly);

            foreach (var a in _dict)
            {
                dict.Add(a.Key, a.Value);
            }
        }
        invalid = false;
    }

    public static void RegisterAssembly(Assembly assembly)
    {
        invalid = true;
        assemblies.Add(assembly);
    }

    public static OptionsFormsLocatorItem? TryGetForOptionType(Type optionType)
    {
        RefreshDict();
        return dict.GetValueOrDefault(optionType);
    }

    public static IEnumerable<OptionsFormsLocatorItem> RegisteredForms()
    {
        RefreshDict();
        return dict.Values.ToList();
    }

    public static IEnumerable<OptionsFormsLocatorItem> RegisteredFormsAutoShow()
    {
        RefreshDict();
        return dict.Values.Where(s => s.IsAutoShowFormOnSettingsPageAttribute).ToList();
    }

    /// <summary>
    /// GetOptionsEditForms
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>Key NodeType; Valye FormType</returns>
    public static Dictionary<Type, OptionsFormsLocatorItem> GetOptionsEditForms(Assembly assembly)
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
