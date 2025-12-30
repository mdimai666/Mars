using System.Linq.Expressions;
using AutoFixture;
using Mars.Integration.Tests;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.Helpers;
using Mars.WebApiClient.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mars.WebApiClient.Integration.Tests;

[Collection("WebApiClientTestApp")]
public class BaseWebApiClientTests : ApplicationTests
{
    public BaseWebApiClientTests(ApplicationFixture appFixture) : base(appFixture)
    {

    }

    public IMarsWebApiClient GetWebApiClient(bool isAnonymous = false)
        => new MarsWebApiClient(AppFixture.ServiceProvider, AppFixture.GetClient(isAnonymous));

    public async Task<T?> GetEntity<T>(Guid id)
        where T : class
    {
        var ef = AppFixture.MarsDbContext();
        var exp = ReflectionHelper.GetIdEqualsExpression<T>(id);
        return await ef.Set<T>().FirstOrDefaultAsync(exp);
    }

    public async Task<List<T>> GetEntities<T>(Guid[] ids)
        where T : class
    {
        var ef = AppFixture.MarsDbContext();
        var exp = ReflectionHelper.GetIdInExpression<T>(ids);

        return await ef.Set<T>().Where(exp).ToListAsync();
    }

    public async Task<T?> GetEntity<T>(Expression<Func<T, bool>> exp)
        where T : class
    {
        var ef = AppFixture.MarsDbContext();
        return await ef.Set<T>().FirstOrDefaultAsync(exp);
    }

    public async Task<T> CreateEntity<T>()
        where T : class
    {
        var created = _fixture.Create<T>()!;
        var ef = AppFixture.MarsDbContext();
        await ef.Set<T>().AddAsync(created);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();
        return created;
    }

    public async Task<IReadOnlyCollection<T>> CreateManyEntities<T>(int count = 3)
        where T : class
    {
        var created = _fixture.CreateMany<T>(count).ToList();
        var ef = AppFixture.MarsDbContext();
        await ef.Set<T>().AddRangeAsync(created);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();
        return created.ToList();
    }

    public async Task<bool> AnyEntities<T>(Guid[] ids)
        where T : class
    {
        var exp = ReflectionHelper.GetAnyByIdsExpression<T>(ids);

        bool exists = await AppFixture.MarsDbContext()
            .Set<T>()
            .AnyAsync(exp);

        return exists;
    }
}
