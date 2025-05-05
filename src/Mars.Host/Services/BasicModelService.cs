using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Security.Claims;
using AppShared.Dto;
using AppShared.Interfaces;
using AppShared.Models;
using AppShared.Resources;
using Mars.Host.Data;
using Mars.Host.Extensions;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Mars.Host.Data.Entities;
using Mars.Host.Data.Contexts;

namespace Mars.Host.Services;

public class BasicModelService<TEntity> : BasicModelService<TEntity, MarsDbContext> where TEntity : class, IBasicEntity, new()
{
    public BasicModelService(IConfiguration configuration, IServiceProvider serviceProvider) : base(serviceProvider)
    {

    }
}

public class BasicModelService<TEntity, TDbContext> : IBasicModelService<TEntity>, IDisposable
    where TEntity : class, IBasicEntity, new()
    where TDbContext : MarsDbContext
{
    protected const int DEFAULT_LIMIT = 20;

    protected readonly UserManager<UserEntity> _userManager;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    protected readonly IServiceProvider _serviceProvider;
    //protected IIdentity _user => _httpContextAccessor.HttpContext.User.Identity;
    protected ClaimsPrincipal _user => _httpContextAccessor.HttpContext.User;
    //protected readonly ILogger _logger;

    //public DbSet<TEntity> Items => ef.Set<TEntity>();

    const bool ALWAYS_USE_NEW_CONTEXT = true;
    protected readonly string _connectionString;

    protected IServiceScope? _scope;

    protected Guid? _userId
    {
        get
        {
            if (_user.Identity.IsAuthenticated == false) return null;

            string id = _userManager.GetUserId(_user);
            if (Guid.TryParse(id, out Guid guid))
            {
                return guid;
            };
            return null;
        }
    }

    public BasicModelService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        if (typeof(TEntity) == typeof(Option))
        {
            _scope = serviceProvider.CreateScope();
            _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();
        }
        else
        {
            _userManager = _serviceProvider.GetRequiredService<UserManager<UserEntity>>();
        }
        _httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
        _connectionString = IOptionService.Configuration.GetConnectionString("DefaultConnection");

        //ILoggerFactory logger = _serviceProvider.GetRequiredService<ILoggerFactory>();
        //_logger = logger.CreateLogger(GetType());
    }

    /// <summary>
    /// Использовать этот метод. Потому как у плагинов другой DBContext
    /// </summary>
    /// <returns></returns>
    public virtual TDbContext GetEFContext()
    {
        //Console.WriteLine("EF+GetEFContext");
        //var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        //optionsBuilder.UseNpgsql(_connectionString);
        //optionsBuilder.EnableDetailedErrors();
        ////optionsBuilder.EnableSensitiveDataLogging();
        //return new TDbContext(optionsBuilder.Options);

        return _serviceProvider.GetRequiredService<TDbContext>();
    }

    async public virtual Task<TEntity> Add(TEntity entity)
    {
        TEntity item;

        using var context = GetEFContext();

        if (entity is IBasicUserEntity userEntity)
        {
            if (userEntity.UserId == Guid.Empty && _user.Identity.IsAuthenticated)
            {
                string userId = _userManager.GetUserId(_user);
                userEntity.UserId = Guid.Parse(userId);
            }
        }

        var result = context.Set<TEntity>().Add(entity);
        int id = await context.SaveChangesAsync();
        item = result.Entity;

        return item;

    }

    async public virtual Task<UserActionResult> Delete(Guid id)
    {

        bool result = false;

        using var context = GetEFContext();

        TEntity item = await context.Set<TEntity>().FirstOrDefaultAsync(s => s.Id == id);
        if (item == null)
        {
            result = false;
        }
        else
        {
            context.Remove(item);

            int state = await context.SaveChangesAsync();
            result = true;
        }

        return new UserActionResult
        {
            Ok = result,
            Message = result ? "Успешно удалено" : "Не удалось удалить"
        };

    }

    async public virtual Task<List<TEntity>> List(Expression<Func<TEntity, bool>> predicate = null, int offset = 0, int limit = DEFAULT_LIMIT)
    {

        List<TEntity> items;

        using (var context = GetEFContext())
        {
            var query = predicate != null
                ? context.Set<TEntity>().Where(predicate)
                : context.Set<TEntity>();
            items = await query.AsNoTracking().Skip(offset).Take(limit).OrderByDescending(s => s.Created).ToListAsync();
        }

        return items;

    }

    async public virtual Task<TotalResponse<TEntity>> ListTable(QueryFilter filter, Expression<Func<TEntity, bool>> predicate = null)
    {
        return await ListTable<TEntity>(filter, predicate);
    }


    //DEFAULT controller method
    async public virtual Task<TotalResponse<TEntity>> ListTable(
        QueryFilter filter,
        Expression<Func<TEntity, bool>> predicate,
        [NotNull] params Expression<Func<TEntity, object>>[] include
        )
    {
        return await Q(filter, predicate, include);
    }


    async protected virtual Task<TotalResponse<TEntity>> Q<TProperty>(
        QueryFilter filter,
        Expression<Func<TEntity, bool>> predicate = null,
        [NotNull] params Expression<Func<TEntity, TProperty>>[] include
    )
    {
        TotalResponse<TEntity> a;

        try
        {
            using (var context = GetEFContext())
            {
                var query = predicate != null ? context.Set<TEntity>().Where(predicate) : context.Set<TEntity>();


                if (include != null)
                {
                    foreach (var inc in include)
                    {
                        query = query.Include(inc);
                    }
                }

                a = await query.QueryTable(filter);
            }
        }
        catch (Exception ex)
        {

            return new TotalResponse<TEntity>
            {
                Message = ex.Message,
                Result = ETotalResponeResult.ERROR,
            };
        }

        return a;
    }



    async protected virtual Task<TotalResponse<TEntityInner>> ListTable<TEntityInner>(
        QueryFilter filter,
        Expression<Func<TEntityInner, bool>> predicate = null
        //,[NotNullAttribute] Expression<Func<TEntityInner, TProperty>> navigationPropertyPath = null

        ) where TEntityInner : class, IBasicEntity, new()
    {

        using (var context = GetEFContext())
        {
            return await context.Set<TEntityInner>().QueryTable(filter, predicate);
        }

    }


    async public virtual Task<TEntity> Get(Guid id)
    {

        TEntity item;
        using (var context = GetEFContext())
        {
            item = await context.Set<TEntity>().FirstOrDefaultAsync(s => s.Id == id);
        }
        return item;
    }

    async public virtual Task<TEntity> Get(Guid id, [NotNull] params Expression<Func<TEntity, object>>[] include)
    {

        TEntity item;
        using (var context = GetEFContext())
        {
            IQueryable<TEntity> query = context.Set<TEntity>();//.FirstOrDefault(s => s.Id == id);

            if (include != null)
            {
                foreach (var inc in include)
                {
                    query = query.Include(inc);
                }
            }

            //item = await context.Set<TEntity>().FirstOrDefaultAsync(s => s.Id == id);
            item = await query.FirstOrDefaultAsync(s => s.Id == id);
        }
        return item;
    }

    public virtual Task<TEntity> Update(Guid id, TEntity entity, Expression<Func<TEntity, object>>[]? include = null)
    {
        if (include is null)
            return _Update(id, entity);
        else
            return _Update(id, entity, include);
    }

    // TODO: combine two updates

    /// <summary>
    /// 
    /// <para>
    /// update array exmpale:
    /// </para>
    /// <code>
    ///     context.Entry(dbFoo.SubFoo).CurrentValues.SetValues(newFoo.SubFoo);
    /// </code>
    /// </summary>
    /// <param name="id"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    async Task<TEntity> _Update(Guid id, TEntity entity)
    {

        TEntity item = null;

        var L = _serviceProvider.GetRequiredService<IStringLocalizer<AppRes>>();

        using (var context = GetEFContext())
        {
            TEntity _item = await context.Set<TEntity>().FirstOrDefaultAsync(s => s.Id == id);

            TEntity old = null;
            if (entity is IWriteChangeHistory)
            {
                old = _item.CopyViaJsonConversion<TEntity>();
            }

            if (_item != null)
            {
                //if(_item is Option _o && entity is Option o)
                //{
                //    _o.Key = o.Key;
                //}

                context.Entry(_item).CurrentValues.SetValues(entity);

                _item.Modified = DateTime.Now;

                EntityEntry<TEntity> e = context.Set<TEntity>().Update(_item);
                int state = await context.SaveChangesAsync();
#if DEBUG
                //_logger.LogWarning($"test Update id={_item.Id}");
#endif
                item = e.Entity;

                if (entity is IWriteChangeHistory)
                {
                    var ac = ActionHistory.GetActionHistory(old, item, L);
                    if (ac != null)
                    {
                        var actionHistoryService = _serviceProvider.GetRequiredService<IActionHistoryService>();
                        await actionHistoryService.Add(ac);
                    }
                }
            }
            else
            {
                throw new ArgumentException($"entity {typeof(TEntity).FullName} ID for update not found");
            }
        }

        return item;

    }
    /// <summary>
    /// Не протестировано
    /// </summary>
    /// <param name="id"></param>
    /// <param name="entity"></param>
    /// <param name="include"></param>
    /// <returns></returns>
    async Task<TEntity> _Update(Guid id, TEntity entity,
        [NotNull] params Expression<Func<TEntity, object>>[] include)
    {

        TEntity item = null;

        using (var context = GetEFContext())
        {
            TEntity _item = await context.Set<TEntity>().IncludeMany(include).FirstOrDefaultAsync(s => s.Id == id);
            //TEntity _item = await context.Set<TEntity>().FirstOrDefaultAsync(s => s.Id == id);

            if (_item != null)
            {

                context.Entry(_item).CurrentValues.SetValues(entity);

                _item.Modified = DateTime.Now;

                foreach (var property in include)
                {
                    //bool does = false;
                    //if (typeof(TEntity) == typeof(Post))
                    //{
                    //    var body = property.Body as MemberExpression;

                    //    if (body != null)
                    //    {
                    //        var propertyInfo = (PropertyInfo)body.Member;
                    //        var propertyType = propertyInfo.PropertyType;

                    //        if(propertyInfo.Name == nameof(Post.FileList))
                    //        {
                    //            does = true;
                    //            Post exist = _item as Post;
                    //            Post _new = entity as Post;
                    //        }
                    //    }
                    //}


                    //if (!does)
                    //{
                    Func<TEntity, object> x = property.Compile();
                    AssignNewValue(_item, property, x(entity));
                    //}

                }

                //EntityEntry<TEntity> e = context.Set<TEntity>().Update(_item);

                int state = await context.SaveChangesAsync();

                //_logger.LogWarning($"test Update id={_item.Id}");

                //item = e.Entity;

                item = _item;
            }
            else
            {
                throw new ArgumentException($"entity {typeof(TEntity).FullName} ID for update not found");
            }
        }

        return item;

    }

    public static void AssignNewValue<TProperty>(TEntity obj, Expression<Func<TEntity, TProperty>> expression, TProperty value)
    {
        ParameterExpression valueParameterExpression = Expression.Parameter(typeof(object));
        Expression targetExpression = expression.Body is UnaryExpression ? ((UnaryExpression)expression.Body).Operand : expression.Body;

        var newValue = Expression.Parameter(expression.Body.Type);
        var assign = Expression.Lambda<Action<TEntity, TProperty>>
                    (
                        Expression.Assign(targetExpression, Expression.Convert(valueParameterExpression, targetExpression.Type)),
                        expression.Parameters.Single(),
                        valueParameterExpression
                    );

        assign.Compile().Invoke(obj, value);
    }

    public async Task<UserEntity> GetCurrentUser()
    {
        if (!_user.Identity.IsAuthenticated) return null;

        var user = await _userManager.GetUserAsync(_user);

        return user;
    }
    
    public async Task<User> GetCurrentUser1()
    {
        if (!_user.Identity.IsAuthenticated) return null;

        var user = await _userManager.GetUserAsync(_user);

        return user.CopyViaJsonConversion<User>();
    }

    //public async Task<UserRoleDto> GetCurrentUserWithRoles(IMarsDbContext ef)
    //{
    //    if (!_user.Identity.IsAuthenticated) return null;

    //    var userService = _serviceProvider.GetRequiredService<IUserService>();

    //    Guid userId = Guid.Parse(_userManager.GetUserId(_user));

    //    var user = await userService.UserWithRolesOne(ef, userId);

    //    return user;
    //}

    public async Task<(UserEntity, bool)> GetCurrentUserIsAdmin()
    {
        if (!_user.Identity.IsAuthenticated) return (null, false);

        var user = await _userManager.GetUserAsync(_user);
        if (user is null) return (null, false);
        bool isAdmin = _userManager.IsInRoleAsync(user, "Admin").Result;

        return (user, isAdmin);
    }

    public static void AssignNewValue2<TModel, TProperty>(TModel obj, Expression<Func<TModel, TProperty>> expression, TProperty value)
    {
        ParameterExpression valueParameterExpression = Expression.Parameter(typeof(TProperty));
        Expression targetExpression = expression.Body is UnaryExpression ? ((UnaryExpression)expression.Body).Operand : expression.Body;

        var newValue = Expression.Parameter(expression.Body.Type);
        var assign = Expression.Lambda<Action<TModel, TProperty>>
                    (
                        Expression.Assign(targetExpression, Expression.Convert(valueParameterExpression, targetExpression.Type)),
                        expression.Parameters.Single(),
                        valueParameterExpression
                    );

        assign.Compile().Invoke(obj, value);
    }

    public async Task<UpdateManyToManyChanges> UpdateManyToMany<TManyToManyEntity>(
        DbSet<TManyToManyEntity> dbset,
        Expression<Func<TManyToManyEntity, bool>> queryExp,
        Expression<Func<TManyToManyEntity, Guid>> mainEntitySelect,
        Expression<Func<TManyToManyEntity, Guid>> subEntitySelect,
        Guid postId,
        IEnumerable<Guid> ids, bool removeUnlessIds = true
    ) where TManyToManyEntity : class, IBasicEntity, new()
    {
        //var x = prop.Compile();
        var xMain = mainEntitySelect.Compile();
        var xSub = subEntitySelect.Compile();
        List<TManyToManyEntity> existList = await dbset.Where(queryExp).ToListAsync();

        IEnumerable<Guid> existIds = existList.Select(s => s.Id); //selfIds
        IEnumerable<Guid> existSubEntityIds = existList.Select(s => xSub(s));

        ImmutableList<Guid> requireAddSubEntityIds = ids.Where(s => existSubEntityIds.Contains(s) == false).ToImmutableList();

        ImmutableList<Guid> requireRemoveSubEntitiesIds = existSubEntityIds.Where(s => ids.Contains(s) == false).ToImmutableList();
        ImmutableList<Guid> requireRemoveIds = existList.Where(s => requireRemoveSubEntitiesIds.Contains(xSub(s)) == true).Select(s => s.Id).ToImmutableList();

        if (removeUnlessIds)
        {
            foreach (var z in requireRemoveIds)
            {
                //TManyToManyEntity a = new();
                //a.Id = z;
                //dbset.Attach(a);
                TManyToManyEntity a = existList.First(s => s.Id == z);
                dbset.Remove(a);
            }
        }

        foreach (var z in requireAddSubEntityIds)
        {
            TManyToManyEntity a = new();
            AssignNewValue2(a, mainEntitySelect, postId);
            AssignNewValue2(a, subEntitySelect, z);

            dbset.Add(a);
        }

        return new UpdateManyToManyChanges
        {
            AddedSubEntityIds = requireAddSubEntityIds,
            RemovedSubEntitiesIds = requireRemoveSubEntitiesIds,
            RemovedIds = requireRemoveIds
        };
    }

    public (ICollection<TFieldEntity> added, ICollection<TFieldEntity> removed) UpdateCollectionField<TFieldEntity>(
        DbSet<TFieldEntity> dbset,
        TEntity entityDbInstance,
        ICollection<TFieldEntity> existList,
        ICollection<TFieldEntity> newList,
        //Expression<Func<TFieldEntity, bool>> queryExp,
        Expression<Func<TFieldEntity, Guid>> subEntity_ParentEntityIdSelect
        )
        where TFieldEntity : class, IBasicEntity, new()
    {
        //var exist = dbset.Where(queryExp).ToList();
        var (added, removed) = ModelServiceTools.CollectionDiffById(existList, newList);

        dbset.RemoveRange(removed);
        dbset.AttachRange(added);
        dbset.AddRange(added);

        foreach (var e in newList)
        {
            AssignNewValue2(e, subEntity_ParentEntityIdSelect, entityDbInstance.Id);
        }

        return (
            added: added,
            removed: removed
        );
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}
