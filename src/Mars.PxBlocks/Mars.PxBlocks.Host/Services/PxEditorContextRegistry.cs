using Mars.PxBlocks.Host.Shared;
using Mars.PxBlocks.Host.Shared.Services;

namespace Mars.PxBlocks.Host.Services;

/// <summary>
/// Реестр контекстов редактора: наполняется при старте приложения
/// (IPxEditorContextRegistry.Register после UsePxBlocks), читается запросами.
/// </summary>
public sealed class PxEditorContextRegistry : IPxEditorContextRegistry
{
    private readonly List<PxEditorContext> _contexts = [];
    private readonly Dictionary<string, PxEditorContext> _byName = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PxEditorContext> Contexts => _contexts;

    public PxEditorContext? Get(string name) => _byName.GetValueOrDefault(name);

    public void Register(PxEditorContext context)
    {
        if (!_byName.TryAdd(context.Name, context))
            throw new InvalidOperationException($"PxEditorContext «{context.Name}» уже зарегистрирован");

        _contexts.Add(context);
    }
}
