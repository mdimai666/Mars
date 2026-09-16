using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Providers.File;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Integration.Tests.FileProviders;

public class FileDatasourceProviderTests
{
    const string Slug = "files";

    const string SalesCsv = """
                            name,age,city
                            ann,35,Yakutsk
                            bob,24,Moscow
                            cid,,
                            """;

    static MemoryDatasourceStore StoreWithSales()
    {
        var store = new MemoryDatasourceStore();
        store.Add(Slug, "sales.csv", SalesCsv);
        return store;
    }

    static FileDatasourceProvider Provider(IDatasourceStore store, Dictionary<string, string>? settings = null)
        => new(new DatasourceConfig
        {
            Kind = DatasourceKind.File,
            Slug = Slug,
            Title = "Files",
            Settings = settings ?? [],
        }, store);

    static byte[] Workbook()
    {
        using var stream = new MemoryStream();

        using (var workbook = new XLWorkbook())
        {
            var first = workbook.Worksheets.Add("First");
            first.Cell(1, 1).Value = "id";
            first.Cell(2, 1).Value = 1;

            var second = workbook.Worksheets.Add("Second");
            second.Cell(1, 1).Value = "code";
            second.Cell(2, 1).Value = "a";

            workbook.SaveAs(stream);
        }

        return stream.ToArray();
    }

    [Fact]
    public async Task Catalog_ListsFilesAndSheets()
    {
        var store = StoreWithSales();
        store.Add(Slug, "book.xlsx", Workbook());
        store.Add(Slug, "notes.txt", "не таблица");

        var catalog = await Provider(store).Catalog();

        catalog.Kind.Should().Be(DatasourceKind.File);
        catalog.SourceName.Should().Be("Files");
        catalog.Groups.Single().Objects.Select(o => o.Id).Should().Equal("book.xlsx:First", "book.xlsx:Second", "sales.csv");
        catalog.Groups.Single().Objects.Should().OnlyContain(o => o.ObjectType == DatasourceObjectType.File);
        catalog.Groups.Single().Objects.Should().OnlyContain(o => o.DefaultLanguage == DatasourceLanguage.Linq);
    }

    [Fact]
    public async Task Catalog_InfersColumnTypes()
    {
        var catalog = await Provider(StoreWithSales()).Catalog();

        var sales = catalog.Groups.Single().Objects.Single();
        sales.Columns.Select(c => c.Name).Should().Equal("name", "age", "city");
        sales.Columns[0].DataTypeName.Should().Be(FileTypeInference.Text);
        sales.Columns[1].DataTypeName.Should().Be(FileTypeInference.BigInt);
        sales.Columns[1].ClrTypeName.Should().Be(typeof(long).FullName);
        sales.Columns.Select(c => c.Ordinal).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Catalog_EmptySourceHasNoGroups()
    {
        var catalog = await Provider(new MemoryDatasourceStore()).Catalog();

        catalog.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task Query_AllRowsWhenNoPredicate()
    {
        var result = await Provider(StoreWithSales()).Query(new DatasourceRequest { ObjectId = "sales.csv" });

        result.Ok.Should().BeTrue(result.Message);
        result.Columns.Select(c => c.Name).Should().Equal("name", "age", "city");
        result.Rows.Should().HaveCount(3);
        result.Rows[0].Should().Equal("ann", "35", "Yakutsk");
        result.Rows[2][1].Should().BeNull();
        result.Truncated.Should().BeFalse();
        result.DatabaseDriver.Should().Be(DatasourceKind.File);
    }

    [Fact]
    public async Task Query_PredicateWithValHelpers()
    {
        var provider = Provider(StoreWithSales());

        var result = await provider.Query(new DatasourceRequest { ObjectId = "sales.csv", Query = "Val.Num(age) > 30" });

        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().HaveCount(1);
        result.Rows[0][0].Should().Be("ann");
    }

    [Fact]
    public async Task Query_ValHelpersAreCaseSensitive()
    {
        // Свои типы Dynamic LINQ ищет с учётом регистра: val.num вместо Val.Num не находится,
        // и вместо внятной ошибки разбор уходит в поиск поля «age» не там.
        var result = await Provider(StoreWithSales()).Query(new DatasourceRequest { ObjectId = "sales.csv", Query = "val.num(age) > 30" });

        result.Ok.Should().BeFalse();
    }

    [Fact]
    public async Task Query_StringPredicateOnTypedColumn()
    {
        var provider = Provider(StoreWithSales());

        (await provider.Query(new DatasourceRequest { ObjectId = "sales.csv", Query = """name == "ann" """ })).Rows.Should().HaveCount(1);
        (await provider.Query(new DatasourceRequest { ObjectId = "sales.csv", Query = """name.StartsWith("b")""" })).Rows.Should().HaveCount(1);
        (await provider.Query(new DatasourceRequest { ObjectId = "sales.csv", Query = "city != null" })).Rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task Query_MaxRowsTruncates()
    {
        var result = await Provider(StoreWithSales()).Query(new DatasourceRequest { ObjectId = "sales.csv", MaxRows = 2 });

        result.Rows.Should().HaveCount(2);
        result.Truncated.Should().BeTrue();
    }

    [Fact]
    public async Task Query_InvalidPredicateReturnsError()
    {
        var result = await Provider(StoreWithSales()).Query(new DatasourceRequest { ObjectId = "sales.csv", Query = "no_such_column == 1" });

        result.Ok.Should().BeFalse();
        result.Message.Should().NotBeNullOrWhiteSpace();
        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Query_NoObjectIdWithSeveralFilesAsksForObject()
    {
        var store = StoreWithSales();
        store.Add(Slug, "other.csv", "a\n1\n");

        var result = await Provider(store).Query(new DatasourceRequest());

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("Укажите объект");
    }

    [Fact]
    public async Task Query_DefaultFileFromSettings()
    {
        var store = StoreWithSales();
        store.Add(Slug, "other.csv", "a\n1\n");

        var provider = Provider(store, new Dictionary<string, string> { [FileSourceSettings.FileKey] = "sales.csv" });

        (await provider.Query(new DatasourceRequest())).Rows.Should().HaveCount(3);
    }

    [Fact]
    public async Task Query_SingleFileNeedsNoObjectId()
    {
        var result = await Provider(StoreWithSales()).Query(new DatasourceRequest());

        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().HaveCount(3);
    }

    [Fact]
    public async Task Query_MissingFileReturnsError()
    {
        var result = await Provider(StoreWithSales()).Query(new DatasourceRequest { ObjectId = "nope.csv" });

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("не найден");
    }

    [Fact]
    public async Task Query_XlsxSheetById()
    {
        var store = new MemoryDatasourceStore();
        store.Add(Slug, "book.xlsx", Workbook());

        var result = await Provider(store).Query(new DatasourceRequest { ObjectId = "book.xlsx:Second" });

        result.Ok.Should().BeTrue(result.Message);
        result.Columns.Single().Name.Should().Be("code");
        result.Rows.Single()[0].Should().Be("a");
    }

    [Fact]
    public async Task Query_UnknownSheetReturnsError()
    {
        var store = new MemoryDatasourceStore();
        store.Add(Slug, "book.xlsx", Workbook());

        var result = await Provider(store).Query(new DatasourceRequest { ObjectId = "book.xlsx:Nope" });

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("не найден");
    }

    [Fact]
    public async Task Modify_Refuses()
    {
        var result = await Provider(StoreWithSales()).Modify(new DatasourceRequest { ObjectId = "sales.csv", Query = "UPDATE" });

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("только для чтения");
    }

    [Fact]
    public void Capabilities_QueryAndBrowseOnly()
    {
        var capabilities = Provider(StoreWithSales()).Capabilities;

        capabilities.CanQuery.Should().BeTrue();
        capabilities.CanBrowse.Should().BeTrue();
        capabilities.CanWrite.Should().BeFalse();
        capabilities.CanManageViews.Should().BeFalse();
    }

    [Fact]
    public void AddDatasourceFile_RegistersProviderFactory()
    {
        var services = new ServiceCollection()
            .AddSingleton<IDatasourceStore, MemoryDatasourceStore>()
            .AddDatasourceFile()
            .BuildServiceProvider();

        var factory = services.GetRequiredService<IEnumerable<IDatasourceProviderFactory>>().Single();

        factory.Kind.Should().Be(DatasourceKind.File);
        factory.Driver.Should().BeEmpty();
        factory.Create(new DatasourceConfig { Kind = DatasourceKind.File, Slug = Slug }).Should().BeOfType<FileDatasourceProvider>();
    }
}

/// <summary>Хранилище источника в памяти: те же правила имён, что у файлового, но без data-корня.</summary>
class MemoryDatasourceStore : IDatasourceStore
{
    readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string slug, string fileName, string content)
        => Add(slug, fileName, Encoding.UTF8.GetBytes(content));

    public void Add(string slug, string fileName, byte[] content)
        => _files[Key(slug, fileName)] = content;

    public IReadOnlyCollection<string> ListDataFiles(string slug)
        => _files.Keys
            .Where(key => key.StartsWith(slug + "/", StringComparison.OrdinalIgnoreCase))
            .Select(key => key[(slug.Length + 1)..])
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public bool DataFileExists(string slug, string fileName) => _files.ContainsKey(Key(slug, fileName));

    public Stream OpenDataFile(string slug, string fileName)
        => DataFileExists(slug, fileName)
            ? new MemoryStream(_files[Key(slug, fileName)])
            : throw new FileNotFoundException(fileName);

    public void WriteDataFile(string slug, string fileName, Stream content)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        _files[Key(slug, fileName)] = buffer.ToArray();
    }

    static string Key(string slug, string fileName) => $"{slug}/{fileName}";
}
