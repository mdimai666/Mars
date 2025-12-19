using System.Reflection;

namespace Mars.Nodes.Workspace.ActionManager;

public class EditorActionLocator
{
    HashSet<Assembly> assemblies = [];
    object _lock = new { };
    bool invalid;
    private readonly List<EditorActionType> _actions = [];

    public IReadOnlyCollection<EditorActionType> Actions { get { if (invalid) RefreshDict(); return _actions; } }
    public IReadOnlyCollection<EditorActionType> HotkeyActions => Actions.Where(s => s.ActiveHotkey is not null).ToList();

    public void RegisterAssembly(Assembly assembly)
    {
        if (assemblies.Contains(assembly)) return;
        assemblies.Add(assembly);
        invalid = true;
    }

    private void RefreshDict(bool force = false)
    {
        if (!invalid && !force) return;

        lock (_lock)
        {
            _actions.Clear();

            foreach (var assembly in assemblies)
            {
                var _dict = GetActionImplements(assembly);

                foreach (var a in _dict)
                {
                    var attr = a.GetCustomAttribute<EditorActionCommandAttribute>();
                    //            ?? throw new ArgumentException("Action must have [EditorActionHotkeyAttribute]"); ;
                    //if (attr is null) continue;
                    //RegisterAction(a);
                    _actions.Add(new EditorActionType(a, attr));
                }
            }
            invalid = false;
        }
    }

    static IEnumerable<Type> GetActionImplements(Assembly assembly)
    {
        var type = typeof(IEditorAction);

        var types = assembly.GetTypes()
                                .Where(p =>
                                    type.IsAssignableFrom(p)
                                    && p.IsPublic
                                    && p.IsClass
                                    && !p.IsAbstract
                                );

        return types;
    }
}
