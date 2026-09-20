using FluentAssertions;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Providers.File;
using Mars.Datasource.Providers.MsSQL;
using Mars.Datasource.Providers.MySQL;
using Mars.Datasource.Providers.PostgreSQL;
using Mars.Datasource.Providers.Rest;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Tests;

/// <summary>
/// Профиль типа источника — контракт между провайдером и UI: по нему форма настроек рисует поля,
/// а рабочая область выбирает язык, подсказки и кнопки. Новый источник обязан описать себя здесь,
/// а не ветками по Kind во фронте.
/// </summary>
public class KindProfileTests
{
    [Theory]
    [InlineData("psql", "public")]
    [InlineData("mssql", "dbo")]
    [InlineData("mysql", "public")]
    public void SqlProfile_DescribesSqlWorld(string driver, string defaultGroup)
    {
        var profile = Profile(driver);

        profile.Kind.Should().Be(DatasourceKind.Sql);
        profile.Driver.Should().Be(driver);
        profile.Label.Should().NotBeNullOrWhiteSpace();
        profile.DefaultLanguage.Should().Be(DatasourceLanguage.Sql);
        profile.EditorLanguage.Should().Be(DatasourceEditorLanguage.Sql);
        profile.DefaultGroup.Should().Be(defaultGroup);
        profile.CollapseGroupsAbove.Should().Be(0);

        profile.Has(DatasourceFeature.Write).Should().BeTrue();
        profile.Has(DatasourceFeature.CellEdit).Should().BeTrue();
        profile.Has(DatasourceFeature.Views).Should().BeTrue();
        profile.Has(DatasourceFeature.TotalCount).Should().BeTrue();
        profile.Has(DatasourceFeature.Document).Should().BeFalse();

        // У sql-источника настройка одна — строка подключения, своих полей провайдер не объявляет.
        profile.DefaultConnectionString.Should().NotBeNullOrWhiteSpace();
        profile.Settings.Should().BeEmpty();
    }

    [Fact]
    public void FileProfile_DeclaresItsSettingsFields()
    {
        var profile = FileDatasourceProfile.Create();

        profile.Kind.Should().Be(DatasourceKind.File);
        profile.Driver.Should().BeEmpty();
        profile.DefaultLanguage.Should().Be(DatasourceLanguage.Linq);
        profile.EditorLanguage.Should().Be(DatasourceEditorLanguage.CSharp);
        profile.Has(DatasourceFeature.Write).Should().BeFalse();

        profile.Settings.Select(field => field.Key)
            .Should().Equal(DatasourceSettings.Files, DatasourceSettings.HasHeaders, DatasourceSettings.Delimiter);

        profile.Settings[0].Editor.Should().Be(DatasourceSettingEditor.Lines);
        profile.Settings[1].Editor.Should().Be(DatasourceSettingEditor.Checkbox);
    }

    [Fact]
    public void RestProfile_DeclaresDocumentAuthAndDiscovery()
    {
        var profile = RestDatasourceProfile.Create();

        profile.Kind.Should().Be(DatasourceKind.Rest);
        profile.DefaultLanguage.Should().Be(DatasourceLanguage.Http);
        profile.OpensAsDocument.Should().BeTrue();
        profile.DocumentName.Should().Be(DatasourceSettings.RequestsDocument);
        profile.DefaultGroup.Should().Be("GET");
        profile.CollapseGroupsAbove.Should().Be(25);
        profile.Has(DatasourceFeature.Document).Should().BeTrue();
        profile.Has(DatasourceFeature.Discover).Should().BeTrue();
        profile.Has(DatasourceFeature.Write).Should().BeTrue();
        profile.Has(DatasourceFeature.Views).Should().BeFalse();

        profile.Settings.Select(field => field.Key).Should().Equal(
            DatasourceSettings.BaseUrl,
            DatasourceSettings.Discovery,
            DatasourceSettings.DiscoveryUrl,
            DatasourceSettings.TimeoutSec,
            DatasourceSettings.Auth);

        var discovery = profile.Settings.Single(field => field.Key == DatasourceSettings.Discovery);
        discovery.Editor.Should().Be(DatasourceSettingEditor.Select);
        discovery.Options.Select(option => option.Value).Should().Equal(RestDiscovery.All);
        discovery.Options.Should().OnlyContain(option => !string.IsNullOrEmpty(option.Label));

        profile.Settings.Single(field => field.Key == DatasourceSettings.Auth)
            .Editor.Should().Be(DatasourceSettingEditor.Auth);
    }

    static DatasourceKindProfile Profile(string driver)
    {
        var factories = new ServiceCollection()
            .AddDatasourcePostgreSql()
            .AddDatasourceMsSql()
            .AddDatasourceMySql()
            .BuildServiceProvider()
            .GetServices<IDatasourceDriverFactory>();

        var factory = factories.Single(item => item.Driver == driver);

        return SqlDatasourceProfile.Create(factory);
    }
}
