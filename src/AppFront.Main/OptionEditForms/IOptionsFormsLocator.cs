using System.Reflection;
using Mars.Shared.Options.Attributes;

namespace AppFront.Main.OptionEditForms;

public interface IOptionsFormsLocator
{
    IReadOnlyDictionary<Type, OptionsFormsLocatorItem> Dict { get; }

    void RegisterAssembly(Assembly assembly);
    OptionsFormsLocatorItem? TryGetForOptionType(Type optionType);
    IEnumerable<OptionsFormsLocatorItem> RegisteredForms();
    IEnumerable<OptionsFormsLocatorItem> RegisteredFormsAutoShow();

}

public record OptionsFormsLocatorItem
{
    public required Type FormType;
    public required Type OptionType;
    public required OptionEditFormForOptionAttribute attribute1;
    public bool IsAutoShowFormOnSettingsPageAttribute;
    public required string DisplayName;
}
