using Mars.Host.Data;
using Mars.Host.Data.Constants;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Data.InMemory;
using Mars.Host.Data.Options;
using Mars.Host.Data.PostgreSQL;
using Mars.Host.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Infrastructure;

public static class MainInfrastructure
{
    public static IServiceCollection AddMarsHostInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));

        var isInMemory = connectionString.StartsWith(DatabaseProviderConstants.InMemoryDb, StringComparison.OrdinalIgnoreCase);

        var connectionOpt = new DatabaseConnectionOpt()
        {
            ConnectionString = connectionString,
            ProviderName = isInMemory ? DatabaseProviderConstants.InMemoryDb : DatabaseProviderConstants.PostgreSQL
        };
        services.AddSingleton(connectionOpt);

        IMarsDbContextFactory factory = isInMemory
                                            ? new MarsDbContextInMemoryFactory(connectionOpt)
                                            : new MarsDbContextPostgreSQLFactory(connectionOpt);
        services.AddSingleton<IMarsDbContextFactory>(factory);

        Action<DbContextOptionsBuilder> actionOptBuilder = options =>
        {
            factory.OptionsBuilderAction(options);
#if DEBUG
            options.EnableSensitiveDataLogging(true);
#endif
        };

        if (isInMemory)
            services.AddDbContext<MarsDbContext>(actionOptBuilder);
        else
            services.AddDbContextPool<MarsDbContext>(actionOptBuilder);

        services.AddIdentity<UserEntity, RoleEntity>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireDigit = false;
        })
            .AddEntityFrameworkStores<MarsDbContext>()
            .AddDefaultTokenProviders();

        services.AddMarsHostData(connectionString!);
        services.AddMarsHostRepositories();

        return services;
    }
}
