using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.PostCategories;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

internal class PostCategoryRepository : IPostCategoryRepository
{
    private readonly MarsDbContext _marsDbContext;
    private bool _disposed;

    IQueryable<PostCategoryEntity> _listAllQuery => _marsDbContext.PostCategories.OrderBy(s => s.SlugPath);

    public PostCategoryRepository(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public async Task<PostCategorySummary?> Get(Guid id, CancellationToken cancellationToken)
                                => (await _marsDbContext.PostCategories.AsNoTracking()
                                        .Include(s => s.PostCategoryType)
                                        .Include(s => s.PostType)
                                        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken))
                                        ?.ToSummary();

    IQueryable<PostCategoryEntity> InternalDetail => _marsDbContext.PostCategories.AsNoTracking()
                                        .Include(s => s.PostCategoryType)
                                        .Include(s => s.MetaValues!)
                                            .ThenInclude(s => s.MetaField)
                                        .Include(s => s.PostType);

    public async Task<PostCategoryDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
                                => (await InternalDetail
                                        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken))
                                        ?.ToDetail();

    public async Task<PostCategoryDetail?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken)
                                => (await InternalDetail
                                        .FirstOrDefaultAsync(s => s.PostCategoryType.TypeName == type
                                                            && EF.Functions.ILike(s.Slug, slug), cancellationToken))
                                        ?.ToDetail();

    public async Task<PostCategoryEditDetail?> GetPostCategoryEditDetail(Guid id, CancellationToken cancellationToken)
                                => (await InternalDetail
                                        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken))
                                        ?.ToEditDetail();

    public async Task<Guid> Create(CreatePostCategoryQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        if (query.Id == null || query.Id == Guid.Empty) query = query with { Id = Guid.NewGuid() };

        Guid[] pathIds;
        string slugPath;

        if (query.ParentId is null)
        {
            pathIds = [query.Id.Value];
            slugPath = '/' + query.Slug;
        }
        else
        {
            var parent = await _marsDbContext.PostCategories.FirstAsync(s => s.Id == query.ParentId);
            pathIds = [.. parent.PathIds, query.Id.Value];
            slugPath = parent.SlugPath + '/' + query.Slug;
        }

        var entity = query.ToEntity(pathIds, slugPath);

        await _marsDbContext.PostCategories.AddAsync(entity, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
        _marsDbContext.Entry(entity).State = EntityState.Detached;

        return entity.Id;
    }

    public async Task Update(UpdatePostCategoryQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        if (query.Id == query.ParentId) throw new ArgumentException();

        var entity = await _marsDbContext.PostCategories.Include(s => s.PostCategoryType)
                                                .Include(s => s.MetaValues!)
                                                    .ThenInclude(s => s.MetaField)
                                                .FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken)
                                                ?? throw new NotFoundException();

        var isParentChanged = entity.ParentId != query.ParentId;
        var graphValuesChanged = isParentChanged || entity.Slug != query.Slug;

        Guid[] pathIds;
        string slugPath;

        if (query.ParentId == null)
        {
            pathIds = [query.Id];
            slugPath = '/' + query.Slug;
        }
        if (isParentChanged)
        {
            var parent = await _marsDbContext.PostCategories.FirstAsync(s => s.Id == query.ParentId);
            pathIds = [.. parent.PathIds, query.Id];
            slugPath = parent.SlugPath + '/' + query.Slug;
        }
        else
        {
            pathIds = entity.PathIds;
            slugPath = entity.SlugPath;
        }

        entity.UpdateEntity(query, pathIds, slugPath);

        if (query.MetaValues is not null)
        {
            MetaValuesTools.ModifyMetaValues(_marsDbContext, entity.MetaValues!, query.MetaValues, entity.ModifiedAt!.Value);
        }

        await _marsDbContext.SaveChangesAsync(cancellationToken);

        if (graphValuesChanged)
            await RecalculateCategoryPathHierarchyFallback(query.PostCategoryTypeId, query.Id, false, cancellationToken);

        _marsDbContext.Entry(entity).State = EntityState.Detached;
    }

    public async Task RecalculateCategoryPathHierarchyFallback(Guid postCategoryTypeId, Guid updatingItemId, bool force, CancellationToken cancellationToken)
    {
        var items = await _marsDbContext.PostCategories.Where(s => s.Id == updatingItemId || s.PathIds.Contains(updatingItemId)).ToDictionaryAsync(s => s.Id, cancellationToken);
        await RecalculateCategoryPathHierarchy(postCategoryTypeId, updatingItemId, force, items, cancellationToken);
    }

    private async Task RecalculateCategoryPathHierarchy(Guid postCategoryTypeId, Guid updatingItemId, bool force, Dictionary<Guid, PostCategoryEntity> processingItems, CancellationToken cancellationToken)
    {
        var items = processingItems;
        var currentItem = items[updatingItemId];
        var childs = items.Values.Where(s => s.ParentId == updatingItemId).Select(s => s.Id).ToList();
        var hasChanges = false;

        if (!force && childs.None()) return;

        if (force && currentItem.ParentId == null)
        {
            currentItem.SlugPath = '/' + currentItem.Slug;
            currentItem.RootId = currentItem.Id;
            currentItem.Path = '/' + currentItem.Id.ToString();
            currentItem.PathIds = [currentItem.Id];
            hasChanges = true;
        }

        void updateFields(PostCategoryEntity item, PostCategoryEntity parent)
        {
            item.SlugPath = parent.SlugPath + '/' + item.Slug;
            item.RootId = parent.RootId;
            item.Path = parent.Path + '/' + item.Id;
            item.PathIds = [.. parent.PathIds, item.Id];
            hasChanges = true;
        }

        foreach (var id in childs)
        {
            var item = items[id];
            updateFields(item, currentItem);
        }

        //Тут обновить внуков надо
        /*
         Root
            childs
                grandsonF1
                    item1
                    item2
                grandsonF2
                    item1
                    ...
         */
        var grandsons = items.Values.Where(s => s.ParentId != null && childs.Contains(s.ParentId!.Value));
        while (grandsons.Any())
        {
            foreach (var item in grandsons)
            {
                var itemParent = items[item.ParentId!.Value];
                updateFields(item, itemParent);
            }
            childs = grandsons.Select(s => s.Id).ToList();
            grandsons = items.Values.Where(s => s.ParentId != null && childs.Contains(s.ParentId!.Value));
        }

        if (hasChanges)
            await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecalculateCategoryPathHierarchyForRoot(Guid postCategoryTypeId, Guid updatingItemId, bool force, CancellationToken cancellationToken)
    {
        var items = await _marsDbContext.PostCategories.Where(s => s.PostCategoryTypeId == postCategoryTypeId).ToDictionaryAsync(s => s.Id, cancellationToken);
        await RecalculateCategoryPathHierarchy(postCategoryTypeId, updatingItemId, force, items, cancellationToken);
    }

    public async Task RecalculateCategoryTypePathHierarchy(Guid postCategoryTypeId, bool force, CancellationToken cancellationToken)
    {
        var rootItems = await _marsDbContext.PostCategories.Where(s => s.PostCategoryTypeId == postCategoryTypeId && s.ParentId == null).ToListAsync(cancellationToken);
        foreach (var rootItem in rootItems)
        {
            await RecalculateCategoryPathHierarchyForRoot(postCategoryTypeId, rootItem.Id, force, cancellationToken);
        }
    }

    public async Task RecalculateAllCategoryPathsHierarchy(CancellationToken cancellationToken)
    {
        var categoryTypes = await _marsDbContext.PostCategoryTypes.ToListAsync(cancellationToken);
        foreach (var item in categoryTypes)
        {
            await RecalculateCategoryTypePathHierarchy(item.Id, true, cancellationToken);
        }
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var entity = await _marsDbContext.PostCategories.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Remove(entity);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> DeleteMany(DeleteManyPostCategoryQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        return _marsDbContext.PostCategories.Where(s => query.Ids.Contains(s.Id)).ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Throws if this class has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedThrowHelper.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Dispose the store
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }

    private IQueryable<PostCategoryEntity> ListAllInternal(ListAllPostCategoryQuery query)
    {
        return _listAllQuery.AsNoTracking()
                            .Include(s => s.PostCategoryType)
                            .Include(s => s.PostType)
                            .Where(s => (query.Ids == null || query.Ids.Contains(s.Id))
                                        && (query.Type == null || s.PostCategoryType.TypeName == query.Type)
                                        && (query.PostTypeName == null || s.PostType.TypeName == query.PostTypeName)
                                    );
    }

    public async Task<IReadOnlyCollection<PostCategorySummary>> ListAll(ListAllPostCategoryQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var list = ListAllInternal(query);

        return (await list.ToListAsync(cancellationToken)).ToSummaryList();
    }

    public async Task<IReadOnlyCollection<PostCategoryDetail>> ListAllDetail(ListAllPostCategoryQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var list = ListAllInternal(query).Include(s => s.MetaValues!)
                                            .ThenInclude(s => s.MetaField);

        return (await list.ToListAsync(cancellationToken)).ToDetailList();
    }

    IQueryable<PostCategoryEntity> ListFilterQuery(ListPostCategoryQuery query) => _listAllQuery
                                    .AsNoTracking()
                                    .Include(s => s.PostCategoryType)
                                    .Include(s => s.PostType)
                                    .Where(s => (query.Type == null || s.PostCategoryType.TypeName == query.Type)
                                            || (query.PostTypeName == null || s.PostType.TypeName == query.PostTypeName))
                                    .Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.Slug, $"%{query.Search}%")
                                        || EF.Functions.ILike(s.Title, $"%{query.Search}%")));

    public async Task<ListDataResult<PostCategorySummary>> List(ListPostCategoryQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query);

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(PostCategoryMapping.ToSummaryList);
    }

    public async Task<PagingResult<PostCategorySummary>> ListTable(ListPostCategoryQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query);

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(PostCategoryMapping.ToSummaryList);
    }

    public async Task<ListDataResult<PostCategoryDetail>> ListDetail(ListPostCategoryQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query).Include(s => s.MetaValues!)
                                            .ThenInclude(s => s.MetaField)
                                            .Include(s => s.PostType);

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(PostCategoryMapping.ToDetailList);
    }

    public async Task<PagingResult<PostCategoryDetail>> ListTableDetail(ListPostCategoryQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query).Include(s => s.MetaValues!)
                                            .ThenInclude(s => s.MetaField)
                                            .Include(s => s.PostType);

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(PostCategoryMapping.ToDetailList);
    }

    //============================
    //Extra methods
    public Task<bool> ExistAsync(Guid id, CancellationToken cancellationToken)
                        => _marsDbContext.PostCategories.AsNoTracking().AnyAsync(s => s.Id == id, cancellationToken);

}
