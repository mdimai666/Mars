using System.Reflection;
using Mars.PxBlocks.Contracts.Services;
using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Core.Definitions;
using Mars.PxBlocks.Core.Toolbox;
using Mars.PxBlocks.Abstractions.Services;

namespace Mars.PxBlocks.Host.Services;

/// <inheritdoc/>
public sealed class PxBlockCatalog : IPxBlockCatalog
{
    private readonly List<PxBlockDefinition> _definitions = [];
    private readonly List<PxToolboxCategory> _toolboxCategories = [];
    private PxToolbox? _toolboxCache;

    /// <summary>Стандартные листья Runtime (математика/логика/текст/print) — уже в локаторе.</summary>
    public PxBlockImplementsLocator Implements { get; } = PxInterpreter.CreateDefaultImplements();

    public IReadOnlyList<PxBlockDefinition> Definitions => _definitions;

    public PxToolbox Toolbox
    {
        get
        {
            if (_toolboxCache == null)
            {
                var toolbox = PxDefaultToolbox.Create();
                toolbox.InsertDomainCategories(_toolboxCategories);
                _toolboxCache = toolbox;
            }

            return _toolboxCache;
        }
    }

    public void RegisterSet(PxBlockSet set)
    {
        _definitions.AddRange(set.Definitions);
        _toolboxCache = null;
    }

    public void RegisterToolboxCategory(PxToolboxCategory category)
    {
        _toolboxCategories.Add(category);
        _toolboxCache = null;
    }

    public void RegisterAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;
            if (!typeof(PxBlockSet).IsAssignableFrom(type))
                continue;
            if (type.GetConstructor(Type.EmptyTypes) == null)
                continue;

            RegisterSet((PxBlockSet)Activator.CreateInstance(type)!);
        }

        Implements.RegisterAssembly(assembly);
    }
}
