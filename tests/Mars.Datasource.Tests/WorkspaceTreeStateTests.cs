using FluentAssertions;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Front.Workspaces;
using Mars.Datasource.Providers.Rest;

namespace Mars.Datasource.Tests;

/// <summary>
/// Сворачивание групп дерева по умолчанию: sql сворачивает не-дефолтную схему всегда,
/// rest — только на большом каталоге (больше <see cref="DatasourceKindProfile.CollapseGroupsAbove"/>
/// операций), раскрытым остаётся GET.
/// </summary>
public class WorkspaceTreeStateTests
{
    [Fact]
    public void SqlCatalog_CollapsesNonDefaultSchema_RegardlessOfSize()
    {
        var state = new WorkspaceTreeState();

        state.ApplyDefaults("db", Catalog(SqlProfile(), ("public", 2), ("other", 1)));

        state.IsExpanded("public").Should().BeTrue();
        state.IsExpanded("other").Should().BeFalse();
    }

    [Fact]
    public void RestCatalog_SmallTreeStaysExpanded()
    {
        var state = new WorkspaceTreeState();

        state.ApplyDefaults("api", Catalog(RestDatasourceProfile.Create(), ("GET", 15), ("POST", 10)));

        state.IsExpanded("GET").Should().BeTrue();
        state.IsExpanded("POST").Should().BeTrue();
    }

    [Fact]
    public void RestCatalog_AboveThreshold_CollapsesEverythingButGet()
    {
        var state = new WorkspaceTreeState();

        state.ApplyDefaults("api", Catalog(RestDatasourceProfile.Create(), ("GET", 20), ("POST", 5), ("DELETE", 1)));

        state.IsExpanded("GET").Should().BeTrue();
        state.IsExpanded("POST").Should().BeFalse();
        state.IsExpanded("DELETE").Should().BeFalse();
    }

    [Fact]
    public void RestCatalog_ExactlyThreshold_StaysExpanded()
    {
        var state = new WorkspaceTreeState();

        state.ApplyDefaults("api", Catalog(RestDatasourceProfile.Create(), ("GET", 20), ("POST", 5)));

        state.IsExpanded("POST").Should().BeTrue();
    }

    [Fact]
    public void RestCatalog_WithoutGetGroup_StaysExpanded()
    {
        var state = new WorkspaceTreeState();

        state.ApplyDefaults("api", Catalog(RestDatasourceProfile.Create(), ("POST", 26)));

        state.IsExpanded("POST").Should().BeTrue();
    }

    static DatasourceKindProfile SqlProfile() => new() { Kind = DatasourceKind.Sql, DefaultGroup = "public" };

    static DatasourceCatalog Catalog(DatasourceKindProfile profile, params (string Group, int Objects)[] groups) => new()
    {
        Profile = profile,
        Groups = groups
            .Select(group => new DatasourceCatalogGroup
            {
                Name = group.Group,
                Objects = Enumerable.Range(0, group.Objects)
                    .Select(index => new DatasourceCatalogObject { Id = $"{group.Group}-{index}", Name = $"item {index}" })
                    .ToList(),
            })
            .ToList(),
    };
}
