using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Mars.CodeCompletion.Host.Services;

/// <summary>
/// Имя типа → пространства имён по всем ссылкам контекста. Нужен, чтобы для
/// неимпортированного типа из completion составить `using`-правку: публичный
/// Roslyn API не отдаёт символ элемента дополнения (SymbolCompletionItem internal,
/// Properties пустые, а GetSymbolsWithName ищет только source-декларации),
/// поэтому обходим метаданные ссылок и строим индекс один раз на контекст.
/// </summary>
public sealed class CompletionTypeIndex
{
    private readonly Dictionary<string, string[]> _namespacesByName;

    private CompletionTypeIndex(Dictionary<string, string[]> namespacesByName)
        => _namespacesByName = namespacesByName;

    public static async Task<CompletionTypeIndex> CreateAsync(
        Project project, ILogger? logger, CancellationToken ct)
    {
        var compilation = await project.GetCompilationAsync(ct)
            ?? throw new InvalidOperationException("Compilation is unavailable for type index");

        var map = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                WalkNamespace(assembly.GlobalNamespace, map);
            }
            catch (Exception e)
            {
                logger?.LogWarning(e, "Type index: failed to walk assembly '{Assembly}'", assembly.Name);
            }
        }

        return new CompletionTypeIndex(
            map.ToDictionary(g => g.Key, g => g.Value.ToArray(), StringComparer.Ordinal));
    }

    private static void WalkNamespace(
        INamespaceSymbol ns, Dictionary<string, SortedSet<string>> map)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (type.DeclaredAccessibility != Accessibility.Public || type.TypeKind == TypeKind.Error)
                continue;

            if (!map.TryGetValue(type.MetadataName, out var namespaces))
                map[type.MetadataName] = namespaces = new SortedSet<string>(StringComparer.Ordinal);
            namespaces.Add(ns.ToDisplayString());
        }

        foreach (var child in ns.GetNamespaceMembers())
            WalkNamespace(child, map);
    }

    /// <summary>Все пространства имён, содержащие публичный тип с данным именем (пусто, если имя неизвестно).</summary>
    public IReadOnlyList<string> GetNamespaces(string metadataName)
        => _namespacesByName.TryGetValue(metadataName, out var namespaces) ? namespaces : [];
}
