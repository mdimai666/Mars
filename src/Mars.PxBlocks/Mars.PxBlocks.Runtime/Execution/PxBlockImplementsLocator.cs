using System.Reflection;

namespace Mars.PxBlocks.Runtime.Execution;

/// <summary>
/// Реестр имплементаций блоков-листьев по TypeId — паттерн NodesLocator из Mars.Nodes:
/// RegisterAssembly сканирует сборку на реализации IPxBlockImplement с конструктором
/// без параметров. Control flow в локатор не попадает — он в ядре PxInterpreter.
/// </summary>
public sealed class PxBlockImplementsLocator
{
    private readonly Dictionary<string, IPxBlockImplement> _dict = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> TypeIds => _dict.Keys;

    public void Register(IPxBlockImplement implement) => _dict[implement.TypeId] = implement;

    public void RegisterAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;
            if (!typeof(IPxBlockImplement).IsAssignableFrom(type))
                continue;
            if (type.GetConstructor(Type.EmptyTypes) == null)
                continue;

            Register((IPxBlockImplement)Activator.CreateInstance(type)!);
        }
    }

    public IPxBlockImplement? Find(string typeId) => _dict.GetValueOrDefault(typeId);
}
