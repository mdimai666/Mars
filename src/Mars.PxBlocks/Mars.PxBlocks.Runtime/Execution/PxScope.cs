using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Execution;

/// <summary>
/// Область видимости переменных: глобальная + рамки функций (цепочка к родителю).
/// Переменные идентифицируются id из workspace Blockly (уникален), не именем.
/// </summary>
internal sealed class PxScope
{
    private readonly Dictionary<string, PxValue> _variables = new(StringComparer.Ordinal);

    public PxScope? Parent { get; }

    public PxScope(PxScope? parent = null) => Parent = parent;

    public bool TryGet(string variableId, out PxValue value)
    {
        for (var scope = this; scope != null; scope = scope.Parent)
        {
            if (scope._variables.TryGetValue(variableId, out value!))
                return true;
        }

        value = PxNullValue.Instance;
        return false;
    }

    public PxValue Get(string variableId)
        => TryGet(variableId, out var value)
            ? value
            : throw new PxRuntimeException($"Переменная не найдена (id={variableId})");

    /// <summary>Привязка в текущей рамке (параметры функций).</summary>
    public void Define(string variableId, PxValue value) => _variables[variableId] = value;

    /// <summary>Присваивание: обновляет существующую привязку по цепочке; не найдена — создаёт в глобальной.</summary>
    public void Set(string variableId, PxValue value)
    {
        for (var scope = this; scope != null; scope = scope.Parent)
        {
            if (scope._variables.ContainsKey(variableId))
            {
                scope._variables[variableId] = value;
                return;
            }
        }

        var root = this;
        while (root.Parent != null)
            root = root.Parent;
        root._variables[variableId] = value;
    }
}
