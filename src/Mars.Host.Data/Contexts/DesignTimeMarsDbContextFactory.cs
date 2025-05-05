using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Mars.Host.Data.Contexts;

//public class DesignTimeMarsDbContextFactory : IDesignTimeDbContextFactory<MarsDbContext>
//{
//    public MarsDbContext CreateDbContext(string[] args)
//    {
//        string connectionString = IOptionService.Configuration.GetConnectionString("DefaultConnection")!;

//        //string connectionString = configuration.GetConnectionString("DefaultConnection")!;

//        var optionsBuilder = new DbContextOptionsBuilder<MarsDbContext>();

//        optionsBuilder.UseNpgsql(connectionString, o =>
//        {
//            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
//            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
//#pragma warning disable CS0618 // Type or member is obsolete
//            NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
//#pragma warning restore CS0618 // Type or member is obsolete
//        });

//        optionsBuilder.UseSnakeCaseNamingConvention();
//        optionsBuilder.EnableDetailedErrors(true);
//#if DEBUG
//        optionsBuilder.EnableSensitiveDataLogging(true);
//#endif
//        return new MarsDbContext(optionsBuilder.Options);
//    }
//}
