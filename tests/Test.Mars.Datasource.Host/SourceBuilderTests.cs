using FluentAssertions;
using Mars.Datasource.Dto;

namespace Test.Mars.Datasource.Host;

public class SourceBuilderTests
{
    [Fact]
    public void SelectDatasourceDto_HelperLinkExist_ShouldExist()
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
