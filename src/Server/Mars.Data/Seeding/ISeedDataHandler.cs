using Mars.Data.Contexts;
using Microsoft.Extensions.Configuration;

namespace Mars.Data.Seeding;

/// <summary>
/// Точка расширения первичного сидинга БД: модули регистрируют свои обработчики,
/// ядро исполняет их по возрастанию <see cref="Order"/> после миграций.
/// </summary>
public interface ISeedDataHandler
{
    int Order { get; }

    Task SeedAsync(MarsDbContext dbContext, IServiceProvider services, IConfiguration configuration);
}
