using Mars.Host.Data;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Infrastructure.Services;
using Mars.Host.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Mars.Host.Infrastructure;

public static class MainInfrastructure
{
    public static IServiceCollection AddMarsHostInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

#pragma warning disable CS0618 // Type or member is obsolete
        NpgsqlConnection.GlobalTypeMapper.UseJsonNet();
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618 // Type or member is obsolete

        Action<DbContextOptionsBuilder> actionOptBuilder = options =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            options.UseSnakeCaseNamingConvention();
            options.EnableDetailedErrors(true);
            //options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

#if DEBUG
            options.EnableSensitiveDataLogging(true);
#endif
        };

        services
            //.AddDbContextPool<IMarsDbContext, IMarsDbContextMock>(actionOptBuilder)
            //.AddDbContextPool<Mars.Host.Data.Contexts.MarsDbContext, ApplicationDbContext>(actionOptBuilder);
            .AddDbContextPool<Mars.Host.Data.Contexts.MarsDbContext>(actionOptBuilder);

        //for legacy timstamp
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        services.AddIdentity<UserEntity, RoleEntity>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireDigit = false;
        })
            .AddEntityFrameworkStores<Mars.Host.Data.Contexts.MarsDbContext>()
            //.AddRoles<RoleEntity>()
            .AddDefaultTokenProviders();

        services.AddMarsHostData(connectionString!);
        services.AddMarsHostRepositories();

        MarsDbContextFactory.ConnectionString = connectionString!;
        services.AddSingleton<IMarsDbContextFactory, MarsDbContextFactory>();

        return services;
    }
}
