using Mars.Data.Constants;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Data.InMemory;
using Mars.Data.Options;
using Mars.Data.PostgreSQL;
using Mars.Data.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Data.Infrastructure;

public static class MainDataInfrastructure
{
    public static IServiceCollection AddMarsDataInfrastructure(this IServiceCollection services, IConfiguration configuration)
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

        services.AddMarsDataRepositories();

        return services;
    }
}
