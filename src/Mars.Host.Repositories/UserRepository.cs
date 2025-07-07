using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Core.Utils;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Dto.Users.Passwords;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

internal class UserRepository : IUserRepository, IDisposable
{
    private readonly MarsDbContext _marsDbContext;
    private readonly UserManager<UserEntity> _userManager;
    private readonly ILookupNormalizer _lookupNormalizer;
    private bool _disposed;

    IQueryable<UserEntity> _listAllQuery => _marsDbContext.Users.OrderByDescending(s => s.CreatedAt);

    public UserRepository(MarsDbContext marsDbContext, UserManager<UserEntity> userManager, ILookupNormalizer lookupNormalizer)
    {
        _marsDbContext = marsDbContext;
        _userManager = userManager;
        _lookupNormalizer = lookupNormalizer;
    }

    public async Task<UserSummary?> Get(Guid id, CancellationToken cancellationToken)
        => (await _marsDbContext.Users.AsNoTracking()
                                        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken))?.ToSummary();

    IQueryable<UserEntity> InternalDetail => _marsDbContext.Users.AsNoTracking()
                                        .Include(s => s.Roles)
                                        .Include(s => s.UserType)
                                        .Include(s => s.MetaValues!)
                                            .ThenInclude(s => s.MetaField);

    public async Task<UserDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
                                => (await InternalDetail
                                        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken))
                                        ?.ToDetail();

    public async Task<UserDetail?> GetDetailByUserName(string username, CancellationToken cancellationToken)
                                => (await InternalDetail
                                        .FirstOrDefaultAsync(s => s.UserName == username, cancellationToken))
                                        ?.ToDetail();

    public async Task<UserEditDetail?> GetUserEditDetail(Guid id, CancellationToken cancellationToken)
                                => (await InternalDetail
                                        .Include(s => s.UserType)
                                            .ThenInclude(s => s.MetaFields!)
                                                .ThenInclude(s => s.MetaValues)
                                        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken))
                                        ?.ToEditDetail();

    public async Task<Guid> Create(CreateUserQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        //var roles = await _marsDbContext.Roles.ToListAsync(cancellationToken);

        var userTypesId = (await _marsDbContext.UserTypes.FirstAsync(s => s.TypeName == query.Type)).Id;

        var user = query.ToEntity(userTypesId, _lookupNormalizer);

        IdentityResult result = await _userManager.CreateAsync(user, query.Password);

        if (!result.Succeeded)
        {
            throw new UserActionException("cannot create user", result.Errors.Select(s => s.Description).ToArray());
        }

        if (query.Roles.Count() > 0)
        {
            var addRolesResult = await _userManager.AddToRolesAsync(user, query.Roles);

            if (!addRolesResult.Succeeded)
            {
                throw new UserActionException("cannot add user to Roles", result.Errors.Select(s => s.Description).ToArray());
            }
        }

        return user.Id;
    }

    public async Task Update(UpdateUserQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = await _marsDbContext.Users.Include(s => s.UserType)
                                                .Include(s => s.Roles)
                                                .Include(s => s.MetaValues!)
                                                    .ThenInclude(s => s.MetaField)
                                                .FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken)
                                                ?? throw new NotFoundException();

        entity.FirstName = query.FirstName;
        entity.LastName = query.LastName ?? "";
        entity.MiddleName = query.MiddleName;
        entity.Email = query.Email;

        entity.PhoneNumber = query.PhoneNumber;
        entity.BirthDate = query.BirthDate;
        entity.Gender = UserMapping.ParseGender(query.Gender);

        var oldRoles = entity.Roles!.Select(s => s.NormalizedName!).Order().ToList();
        var newRoles = query.Roles.Select(s => _lookupNormalizer.NormalizeName(s)).Order().ToList();

        if (oldRoles.Count != newRoles.Count || oldRoles.SequenceEqual(newRoles))
        {
            var diff = DiffList.FindDifferences(oldRoles!, newRoles);

            if (diff.HasChanges)
            {
                if (diff.ToRemove.Any()) await _userManager.RemoveFromRolesAsync(entity, diff.ToRemove);
                if (diff.ToAdd.Any()) await _userManager.AddToRolesAsync(entity, diff.ToAdd);
            }
        }

        entity.ModifiedAt = DateTimeOffset.Now;
        MetaValuesTools.ModifyMetaValues(_marsDbContext, entity.MetaValues!, query.MetaValues, entity.ModifiedAt.Value);

        if (entity.UserType.TypeName != query.Type)
        {
            var newUserType = await _marsDbContext.UserTypes.FirstAsync(s => s.TypeName == query.Type);
            entity.UserTypeId = newUserType.Id;
        }

        await _marsDbContext.SaveChangesAsync(cancellationToken);
        await _userManager.UpdateSecurityStampAsync(entity).ConfigureAwait(false);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var entity = await _marsDbContext.Users.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Remove(entity);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
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

    private Task<List<UserEntity>> ListAllInternal(ListAllUserQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return _listAllQuery.AsNoTracking()
            .Where(s => query.Ids == null || query.Ids.Contains(s.Id))
            .ToListAsync(cancellationToken);
    }

    private Task<List<UserEntity>> ListAllDetailInternal(ListAllUserQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return _listAllQuery.AsNoTracking()
            .Where(s => query.Ids == null || query.Ids.Contains(s.Id))
            .Include(s => s.Roles)
            .Include(s => s.UserType)
            .Include(s => s.MetaValues!)
                .ThenInclude(s => s.MetaField)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserSummary>> ListAll(ListAllUserQuery query, CancellationToken cancellationToken)
        => (await ListAllInternal(query, cancellationToken)).ToSummaryList();

    public async Task<IReadOnlyCollection<UserDetail>> ListAllDetail(ListAllUserQuery query, CancellationToken cancellationToken)
        => (await ListAllDetailInternal(query, cancellationToken)).ToDetailList();

    private IQueryable<UserEntity> ListAllInternal(ListUserQuery query)
    {
        var queryable = _listAllQuery.AsNoTracking()
                            .Include(s => s.UserType)
                            .Include(s => s.MetaValues!)
                                .ThenInclude(s => s.MetaField)
                            .Where(s => query.Search == null
                                    || (s.Id.ToString() == query.Search
                                        || (s.UserName != null && EF.Functions.ILike(s.UserName, $"%{query.Search}%"))
                                        || EF.Functions.ILike(s.FirstName, $"%{query.Search}%")
                                        || EF.Functions.ILike(s.LastName, $"%{query.Search}%")
                                       ));

        //if (query.Sort is not null && query.Sort.TrimStart('-').Equals("Role", StringComparison.OrdinalIgnoreCase))
        //{
        //    var isDescending = query.Sort.StartsWith('-');
        //    query = query with { Sort = null };
        //    queryable = queryable.Include(s => s.Roles);
        //    queryable = isDescending ? queryable.OrderByDescending(s => EF.Functions. s.Roles.FirstOrDefault);
        //}

        if (query.Roles?.Any() ?? false)
        {
            queryable = queryable.Include(s => s.Roles)
                                    //.Where(s=>s.Roles.Any(x=>EF.Functions.ILike(x.Name, x.Name)))
                                    .Where(entity => entity.Roles!.All(role => query.Roles.Contains(role.Name)));
        }

        return queryable;
    }

    private IQueryable<UserEntity> ListAllDetailInternal(ListUserQuery query)
        => ListAllInternal(query).Include(s => s.Roles)
                                    .Include(s => s.UserType);

    public async Task<ListDataResult<UserSummary>> List(ListUserQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListAllInternal(query);

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(UserMapping.ToSummaryList);

    }

    public async Task<ListDataResult<UserDetail>> ListDetail(ListUserQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListAllDetailInternal(query);

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(UserMapping.ToDetail);

    }

    public async Task<PagingResult<UserSummary>> ListTable(ListUserQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListAllInternal(query);

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(UserMapping.ToSummaryList);

    }

    public async Task<PagingResult<UserDetail>> ListTableDetail(ListUserQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListAllDetailInternal(query);

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(UserMapping.ToDetailList);

    }

    // -----------------------

    public async Task<UserActionResult> SetPassword(SetUserPasswordQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var user = await _userManager.FindByEmailAsync(query.Username) ?? throw new NotFoundException();

        var removeResult = await _userManager.RemovePasswordAsync(user);
        var addResult = await _userManager.AddPasswordAsync(user, query.NewPassword);

        return UserActionResult.Success("Пароль успешно изменен");
    }

    public async Task<UserActionResult> SetPassword(SetUserPasswordByIdQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var user = await _userManager.FindByIdAsync(query.UserId.ToString()) ?? throw new NotFoundException();

        var removeResult = await _userManager.RemovePasswordAsync(user);
        var addResult = await _userManager.AddPasswordAsync(user, query.NewPassword);

        return UserActionResult.Success("Пароль успешно изменен");
    }

    public async Task<UserEditProfileDto?> UserEditProfileGet(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var user = await _marsDbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        //MetaFieldService metaFieldService = _serviceProvider.GetRequiredService<MetaFieldService>();
        //var user = Get(id, s => s.MetaValues).Result;
        //using var ef = GetEFContext();
        //var metaFields = UserMetaFields(ef);
        //user.MetaValues = metaFieldService.GetValuesBlank(user.MetaValues, metaFields);
        //user.MetaFields = metaFields;
        //return new UserEditProfileDto(user);

        return user?.ToProfile();
    }

    public async Task<UserActionResult> UpdateUserRoles(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var user = await _marsDbContext.Users.Include(s => s.Roles).AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId) ?? throw new NotFoundException();

        if (user.Roles is null) throw new UserActionException("не удалось получить роли");

        var existRoles = user.Roles!.Select(s => s.Name!).ToList();

        var diff = DiffList.FindDifferences(existRoles!, roles);

        if (!diff.HasChanges) return UserActionResult.Success("нет изменений");

        if (diff.ToRemove.Any()) await _userManager.RemoveFromRolesAsync(user, diff.ToRemove);
        if (diff.ToAdd.Any()) await _userManager.AddToRolesAsync(user, diff.ToAdd);

        await _userManager.UpdateSecurityStampAsync(user).ConfigureAwait(false);

        return UserActionResult.Success("успешно");
    }
}
