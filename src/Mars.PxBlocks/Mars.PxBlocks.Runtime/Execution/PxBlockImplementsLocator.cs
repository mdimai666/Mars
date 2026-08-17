using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mars.PxBlocks.Runtime.Execution;

/// <summary>
/// Реестр ТИПОВ имплементаций блоков-листьев по TypeId (паттерн NodesLocator из
/// Mars.Nodes, но экземпляры не кэшируются): имплементации создаются В МОМЕНТ
/// ЗАПУСКА — по экземпляру на исполнение, состояние запуска допустимо держать в
/// полях. Control flow в локатор не попадает — он в ядре PxInterpreter.
/// </summary>
public sealed class PxBlockImplementsLocator
{
    private readonly Dictionary<string, Type> _types = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> TypeIds => _types.Keys;

    /// <summary>Зарегистрирована ли имплементация (проверка без создания экземпляра).</summary>
    public bool Knows(string typeId) => _types.ContainsKey(typeId);

    /// <summary>
    /// Регистрация типа. TypeId читается пробой: экземпляр без конструктора
    /// (свойство-литерал), иначе — создание с посильными аргументами (TypeId из
    /// базового конструктора, как у стандартных листьев).
    /// </summary>
    public void Register(Type type)
    {
        if (!typeof(IPxBlockImplement).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
            throw new ArgumentException($"Type '{type.Name}' is not an IPxBlockImplement implementation", nameof(type));

        var typeId = ProbeTypeId(type);
        if (string.IsNullOrEmpty(typeId))
            throw new InvalidOperationException($"Implementation '{type.Name}': failed to read TypeId during registration");

        _types[typeId] = type;
    }

    private static string? ProbeTypeId(Type type)
    {
        IPxBlockImplement probe;
        try
        {
            probe = (IPxBlockImplement)RuntimeHelpers.GetUninitializedObject(type);
            if (!string.IsNullOrEmpty(probe.TypeId))
                return probe.TypeId;
        }
        catch
        {
            // Тип не пережил создание без конструктора — пробуем конструкторы.
        }

        foreach (var constructor in type.GetConstructors())
        {
            var parameters = constructor.GetParameters();
            if (parameters.Any(p => p.ParameterType.IsValueType))
                continue;

            try
            {
                probe = (IPxBlockImplement)constructor.Invoke(new object?[parameters.Length]);
                if (!string.IsNullOrEmpty(probe.TypeId))
                    return probe.TypeId;
            }
            catch
            {
                // Проба с null-аргументами не удалась — пробуем следующий конструктор.
            }
        }

        return null;
    }

    /// <summary>Скан сборки на реализации IPxBlockImplement (конструктор — любой публичный).</summary>
    public void RegisterAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;
            if (!typeof(IPxBlockImplement).IsAssignableFrom(type))
                continue;

            Register(type);
        }
    }

    /// <summary>
    /// Создать имплементацию для запуска: конструктор с одним параметром, совместимым
    /// с состоянием запуска (инъекция), иначе конструктор без параметров.
    /// </summary>
    public IPxBlockImplement Create(string typeId, object? state = null)
    {
        if (!_types.TryGetValue(typeId, out var type))
            throw new InvalidOperationException($"Implementation '{typeId}' is not registered");

        if (state != null)
        {
            foreach (var constructor in type.GetConstructors())
            {
                if (constructor.GetParameters() is [var parameter]
                    && parameter.ParameterType.IsAssignableFrom(state.GetType()))
                {
                    return (IPxBlockImplement)constructor.Invoke([state]);
                }
            }
        }

        if (type.GetConstructor(Type.EmptyTypes) != null)
            return (IPxBlockImplement)Activator.CreateInstance(type)!;

        throw new InvalidOperationException(
            $"Implementation '{typeId}': no parameterless constructor or constructor with a run-state parameter");
    }
}
