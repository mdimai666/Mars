using Mars.Datasource.Core;
using Mars.Integration.Tests.Common;

namespace Mars.Datasource.Integration.Tests.Engines;

public class EnginesTests : BaseDatasourceTestAppTests
{
    public static Dictionary<string, DatasourceConfig> configs = new Dictionary<string, DatasourceConfig>()
    {
        ["psql"] = new DatasourceConfig
        {
            Driver = "psql",
            Slug = "psql",
            Title = "My postgres",
            ConnectionString = "Host=127.0.0.1;Database=yago2;Username=postgres;Password=ggxxrr;Port=5432",
        },
        ["mssql"] = new DatasourceConfig
        {
            Driver = "mssql",
            Slug = "mssql",
            Title = "My SQLServer",
            ConnectionString = "Server=.\\SQLEXPRESS;Database=SakhaLISP;User ID=sa;Password=123456;Trusted_Connection=True;TrustServerCertificate=True",
        },
        ["mysql"] = new DatasourceConfig
        {
            Driver = "mysql",
            Slug = "mysql",
            Title = "MySQL Server",
            ConnectionString = "server=127.0.0.1;database=wordpress;uid=wp;pwd=wp;port=3306;Connection Timeout=2;persistsecurityinfo=True;SslMode=none"
            //ConnectionString = "server=127.0.0.1;uid=root;database=wordpress;pwd="
        },
    };

    public EnginesTests(ApplicationFixture appFixture) : base(appFixture)
    {
        // first create containers apps
        throw new NotImplementedException();
    }

    //[Fact]
    //public void TestDataSources()
    //{
    //    var serviceProvider = AppFixture.ServiceProvider;
    //    var cfg = serviceProvider.GetRequiredService<IConfiguration>();
    //    var ops = serviceProvider.GetRequiredService<IOptionService>();
    //    var dbs = serviceProvider.GetRequiredService<IDatabaseBackupService>();
    //    DatasourceOption datasourceOption = new DatasourceOption()
    //    {
    //        Configs = configs.Values.ToList()
    //    };
    //    ops.SetOptionOnMemory(datasourceOption);
    //    //var ds = _serviceProvider.GetRequiredService<DatasourceService>();
    //    var ds = new DatasourceService(cfg, ops, dbs);

    //    foreach (var opt in configs.Values)
    //    {
    //        var tables = ds.Tables(opt.Slug).Result;
    //        Assert.True(tables.Count > 0);
    //    }
    //}
}
