using Mars.PxBlocks.Contracts;
using Mars.PxBlocks.Contracts.Services;
using Mars.PxBlocks.Abstractions.Services;
using Mars.PxBlocks.Abstractions;

namespace Mars.PxBlocks.Host.Services;

/// <inheritdoc/>
public sealed class PxEditorContextRegistry : IPxEditorContextRegistry
{
    private readonly List<PxEditorContext> _contexts = [];
    private readonly Dictionary<string, PxEditorContext> _byName = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PxEditorContext> Contexts => _contexts;

    public PxEditorContext? Get(string name) => _byName.GetValueOrDefault(name);

    public void Register(PxEditorContext context)
    {
        if (!_byName.TryAdd(context.Name, context))
            throw new InvalidOperationException($"PxEditorContext '{context.Name}' is already registered");

        _contexts.Add(context);
    }
}
