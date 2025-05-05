using Mars.Datasource.Core;
using Mars.Datasource.Host.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Mars.Datasource.Integration.Tests.Engines;

public class DriverPostgreSQLTests
{
#if DEBUG
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

    //Dictionary<string, DatasourceConfig> configs => UnitTestDifferentEngines.configs;  
#endif

    [Fact]
    public async Task QueryAsJson()
    {
        var se = new DatasourcePostgreSQLDriver(configs["psql"]);
        //string query = @"SELECT ""Id"",""Title"" FROM ""posts"" LIMIT 10;";
        string query = @"SELECT ""posts"".""Id"" as x, * FROM ""posts"" INNER JOIN ""AspNetUsers"" ON ""posts"".""UserId"" = ""AspNetUsers"".""Id"" LIMIT 10;";

        var res = await se.SqlQueryJson(query);

        if (!res.Ok)
        {
            Assert.Fail(res.Message);
        }
        Assert.NotNull(res.Data);
        Assert.True(res.Data.Count() > 9);
    }

    [Fact]
    public void CheckEfClass()
    {
        //TestMarsDbContext ef = default!;
        //var conn = ef.Database.GetDbConnection();
        DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder<DbContext>();
        optionsBuilder.UseNpgsql(configs["psql"].ConnectionString);
        using DbContext db = new DbContext(optionsBuilder.Options);

        using System.Data.Common.DbConnection conn = db.Database.GetDbConnection();

        conn.Open();

        using var schema = conn.GetSchema("Tables");
        string databaseName = configs["psql"].GetDatabaseName();
        string tableName = "posts";
        var dtCols = conn.GetSchema("Columns", new[] { databaseName, null, tableName });


        //schema.ta
        //var s = new sqlcommand("", conn);
        //var reader = s.ExecuteReader();


        var columns = schema.Columns;

        conn.Close();
    }
}
