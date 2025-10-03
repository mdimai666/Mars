using System.Reflection;
using Mars.Shared.Contracts.XActions;

namespace Mars.Host.Managers;

public class ActLocator
{
    private readonly Dictionary<string, ActLocatorItem> dict = [];
    private bool invalide = true;
    private List<Assembly> assemblies = [];

    public void RefreshDict(bool force = false)
    {
        if (!invalide && !force) return;
        dict.Clear();

        foreach (var assembly in assemblies)
        {
            var _dict = ExtractTypesWithAttributes(assembly);

            foreach (var a in _dict)
            {
                dict.Add(a.Key, a.Value);
            }
        }
    }

    public void RegisterAssembly(Assembly assembly)
    {
        assemblies.Add(assembly);
    }

    public ActLocatorItem? TryGetActionById(string actionId)
    {
        return dict.GetValueOrDefault(actionId);
    }

    public IReadOnlyCollection<ActLocatorItem> ActItems => dict.Values;

    private static Dictionary<string, ActLocatorItem> ExtractTypesWithAttributes(Assembly assembly)
    {
        var type = typeof(IAct);

        var types =
            assembly.GetTypes()
            .Where(p =>
                type.IsAssignableFrom(p)
                && p.IsPublic
                && p.IsClass
                && !p.IsAbstract
            );

        var dict = new Dictionary<string, ActLocatorItem>();

        foreach (var itemType in types)
        {
            var attribute = itemType.GetCustomAttribute<RegisterXActionCommandAttribute>();

            if (attribute != null)
            {
                var item = new ActLocatorItem
                {
                    Attr = attribute,
                    ActType = itemType,
                };
                dict.Add(attribute.ActionId, item);
            }

        }

        return dict;
    }
}

public record ActLocatorItem
{
    public required Type ActType;
    public required RegisterXActionCommandAttribute Attr;
}

public record ActContext : IActContext
{
    public string[] args { get; init; } = [];
}
