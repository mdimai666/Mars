using FluentAssertions;
using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Host.Services;
using Mars.Storage.Services;

namespace Mars.Datasource.Integration.Tests;

/// <summary>
/// Служебные тела источника в data-корне: документ запросов и каталог discovery
/// (<c>data/datasource/&lt;slug&gt;/…</c>).
/// </summary>
public class DatasourceStoreTests
{
    const string Slug = "wp";

    [Fact]
    public async Task WriteThenReadRoundTrips()
    {
        var storage = new InMemoryFileStorage();
        var store = new DatasourceStore(storage);

        await store.WriteTextAsync(Slug, DatasourceSettings.RequestsDocument, "GET /wp/v2/posts");

        (await store.ReadTextAsync(Slug, DatasourceSettings.RequestsDocument)).Should().Be("GET /wp/v2/posts");
    }

    [Fact]
    public async Task ReadMissingFileGivesNull()
    {
        var store = new DatasourceStore(new InMemoryFileStorage());

        (await store.ReadTextAsync(Slug, DatasourceSettings.CatalogDocument)).Should().BeNull();
        store.Exists(Slug, DatasourceSettings.CatalogDocument).Should().BeFalse();
    }

    [Fact]
    public async Task FileLiesUnderDatasourceSlug()
    {
        var storage = new InMemoryFileStorage();
        var store = new DatasourceStore(storage);

        await store.WriteTextAsync(Slug, DatasourceSettings.CatalogDocument, "[]");

        storage.FileExists($"datasource/{Slug}/{DatasourceSettings.CatalogDocument}").Should().BeTrue();
        store.Exists(Slug, DatasourceSettings.CatalogDocument).Should().BeTrue();
    }

    [Fact]
    public async Task WriteReplacesContent()
    {
        var store = new DatasourceStore(new InMemoryFileStorage());

        await store.WriteTextAsync(Slug, DatasourceSettings.RequestsDocument, "старое");
        await store.WriteTextAsync(Slug, DatasourceSettings.RequestsDocument, "новое");

        (await store.ReadTextAsync(Slug, DatasourceSettings.RequestsDocument)).Should().Be("новое");
    }

    [Fact]
    public async Task SourcesAreIsolatedBySlug()
    {
        var store = new DatasourceStore(new InMemoryFileStorage());

        await store.WriteTextAsync("wp", DatasourceSettings.RequestsDocument, "wordpress");
        await store.WriteTextAsync("shop", DatasourceSettings.RequestsDocument, "shop");

        (await store.ReadTextAsync("wp", DatasourceSettings.RequestsDocument)).Should().Be("wordpress");
        (await store.ReadTextAsync("shop", DatasourceSettings.RequestsDocument)).Should().Be("shop");
    }

    [Theory]
    [InlineData("../secrets.txt")]
    [InlineData("a/../../b")]
    [InlineData("./x")]
    public async Task NameOutsideSourceFolderIsRejected(string name)
    {
        var store = new DatasourceStore(new InMemoryFileStorage());

        var write = () => store.WriteTextAsync(Slug, name, "x");

        await write.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EmptyNameIsRejected()
    {
        var store = new DatasourceStore(new InMemoryFileStorage());

        var read = () => store.ReadTextAsync(" ", DatasourceSettings.RequestsDocument);

        await read.Should().ThrowAsync<ArgumentException>();
    }
}
