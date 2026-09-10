using FluentAssertions;
using Mars.Datasource.Abstractions.Models;

namespace Mars.Datasource.Integration.Tests;

public class SourceBuilderTests
{
    [Fact]
    public void SelectDatasourceDto_HelperLinkExist_Exists()
    {
        _ = nameof(SelectDatasourceDto);

        string[] keys = ["psql", "mssql", "mysql"];
        foreach (var key in keys)
        {
            var instance = new SelectDatasourceDto() { Driver = key };
            instance.HelpLinkConnectionString.Should().NotBeNullOrEmpty();
        }
    }
}
