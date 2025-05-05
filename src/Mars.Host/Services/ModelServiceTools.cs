using Mars.Core.Utils;
using Mars.Shared.Models.Interfaces;

namespace Mars.Host.Services;

/// <summary>
/// instead use <see cref="DiffList"/>
/// </summary>
[Obsolete]
public static class ModelServiceTools
{
    public static (ICollection<TEntity1> added, ICollection<TEntity1> removed) CollectionDiffById<TEntity1>(ICollection<TEntity1> existList, ICollection<TEntity1> newList)
        where TEntity1 : class, IBasicEntity
    {
        var existIds = existList.Select(s => s.Id);
        var newListIds = newList.Select(s => s.Id);

        return (
            added: newList.Where(s => !existIds.Contains(s.Id)).ToList(),
            removed: existList.Where(s => !newListIds.Contains(s.Id)).ToList()
        );
    }
}

public class UpdateManyToManyChanges
{
    public required IReadOnlyCollection<Guid> AddedSubEntityIds { get; init; }
    public required IReadOnlyCollection<Guid> RemovedSubEntitiesIds { get; init; }
    public required IReadOnlyCollection<Guid> RemovedIds { get; init; }
}
