using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Providers.File;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Tests.FileProviders;

public class FileDatasourceProviderTests
{
    const string Slug = "files";
    const string SalesCsv = "name,age,city\nann,35,Yakutsk\nbob,24,Moscow\ncid,,\n";

    static MemoryFileSource SourceWithSales()
    {
        var source = new MemoryFileSource();
        source.Add("sales.csv", SalesCsv);
        return source;
    }

    static FileDatasourceProvider Provider(IDatasourceFileSource source, string files = "sales.csv", Dictionary<string, string>? settings = null)
    {
        settings ??= [];
        settings[DatasourceSettings.Files] = files;

        return new FileDatasourceProvider(new DatasourceConfig
        {
            Kind = DatasourceKind.File,
            Slug = Slug,
            Title = "Files",
            Settings = settings,
        }, source);
    }

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
        var source = SourceWithSales();
        source.Add("2026/book.xlsx", Workbook());
        source.Add("notes.txt", "не таблица");

        var catalog = await Provider(source, "sales.csv;2026/book.xlsx;notes.txt").Catalog();

        catalog.Profile.Kind.Should().Be(DatasourceKind.File);
        catalog.SourceName.Should().Be("Files");
        catalog.Groups.Single().Objects.Select(o => o.Id)
            .Should().Equal("sales.csv", "2026/book.xlsx#First", "2026/book.xlsx#Second");
        catalog.Groups.Single().Objects.Select(o => o.Name)
            .Should().Equal("sales.csv", "book.xlsx · First", "book.xlsx · Second");
        catalog.Groups.Single().Objects.Should().OnlyContain(o => o.ObjectType == DatasourceObjectType.File);
        catalog.Groups.Single().Objects.Should().OnlyContain(o => o.DefaultLanguage == DatasourceLanguage.Linq);
    }

    [Fact]
    public async Task Catalog_InfersColumnTypes()
    {
        var catalog = await Provider(SourceWithSales()).Catalog();

        var sales = catalog.Groups.Single().Objects.Single();
        sales.Fields.Select(c => c.Name).Should().Equal("name", "age", "city");
        sales.Fields[0].DataTypeName.Should().Be(FileTypeInference.Text);
        sales.Fields[1].DataTypeName.Should().Be(FileTypeInference.BigInt);
        sales.Fields[1].ClrTypeName.Should().Be(typeof(long).FullName);
        sales.Fields.Select(c => c.Ordinal).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Catalog_NoFilesConfigured_HasNoGroups()
    {
        var catalog = await Provider(SourceWithSales(), files: "").Catalog();

        catalog.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task Query_AllRowsWhenNoPredicate()
    {
        var result = await Provider(SourceWithSales()).Query(new DatasourceRequest { ObjectId = "sales.csv" });

        result.Ok.Should().BeTrue(result.Message);
        result.Fields.Select(c => c.Name).Should().Equal("name", "age", "city");
        result.Rows.Should().HaveCount(3);
        result.Rows[0].Should().Equal("ann", "35", "Yakutsk");
        result.Rows[2][1].Should().BeNull();
        result.Truncated.Should().BeFalse();
        result.Kind.Should().Be(DatasourceKind.File);
    }

    [Fact]
    public async Task Query_PredicateWithValHelpers()
    {
        var result = await Provider(SourceWithSales())
            .Query(new DatasourceRequest { ObjectId = "sales.csv", Query = "Val.Num(age) > 30" });

        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().HaveCount(1);
        result.Rows[0][0].Should().Be("ann");
    }

    [Fact]
    public async Task Query_ValHelpersAreCaseSensitive()
    {
        // Свои типы Dynamic LINQ ищет с учётом регистра: val.num вместо Val.Num не находится,
        // и вместо внятной ошибки разбор уходит в поиск поля «age» не там.
        var result = await Provider(SourceWithSales())
            .Query(new DatasourceRequest { ObjectId = "sales.csv", Query = "val.num(age) > 30" });

        result.Ok.Should().BeFalse();
    }

    [Fact]
    public async Task Query_StringPredicateOnTypedColumn()
    {
        var provider = Provider(SourceWithSales());

        (await provider.Query(new DatasourceRequest { ObjectId = "sales.csv", Query = """name == "ann" """ })).Rows.Should().HaveCount(1);
        (await provider.Query(new DatasourceRequest { ObjectId = "sales.csv", Query = """name.StartsWith("b")""" })).Rows.Should().HaveCount(1);
        (await provider.Query(new DatasourceRequest { ObjectId = "sales.csv", Query = "city != null" })).Rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task Query_MaxRowsTruncates()
    {
        var result = await Provider(SourceWithSales()).Query(new DatasourceRequest { ObjectId = "sales.csv", MaxRows = 2 });

        result.Rows.Should().HaveCount(2);
        result.Truncated.Should().BeTrue();
    }

    [Fact]
    public async Task Query_InvalidPredicateReturnsError()
    {
        var result = await Provider(SourceWithSales())
            .Query(new DatasourceRequest { ObjectId = "sales.csv", Query = "no_such_column == 1" });

        result.Ok.Should().BeFalse();
        result.Message.Should().NotBeNullOrWhiteSpace();
        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Query_NoObjectIdWithSeveralFilesAsksForObject()
    {
        var source = SourceWithSales();
        source.Add("other.csv", "a\n1\n");

        var result = await Provider(source, "sales.csv;other.csv").Query(new DatasourceRequest());

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("Укажите объект");
    }

    [Fact]
    public async Task Query_SingleFileNeedsNoObjectId()
    {
        var result = await Provider(SourceWithSales()).Query(new DatasourceRequest());

        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().HaveCount(3);
    }

    [Fact]
    public async Task Query_NoFilesConfiguredReturnsError()
    {
        var result = await Provider(SourceWithSales(), files: "").Query(new DatasourceRequest());

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("не указано ни одного файла");
    }

    [Fact]
    public async Task Query_MissingFileReturnsError()
    {
        var result = await Provider(SourceWithSales()).Query(new DatasourceRequest { ObjectId = "nope.csv" });

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("не найден");
    }

    [Fact]
    public async Task Query_XlsxSheetById()
    {
        var source = new MemoryFileSource();
        source.Add("2026/book.xlsx", Workbook());

        var result = await Provider(source, "2026/book.xlsx")
            .Query(new DatasourceRequest { ObjectId = "2026/book.xlsx#Second" });

        result.Ok.Should().BeTrue(result.Message);
        result.Fields.Single().Name.Should().Be("code");
        result.Rows.Single()[0].Should().Be("a");
    }

    [Fact]
    public async Task Query_UnknownSheetReturnsError()
    {
        var source = new MemoryFileSource();
        source.Add("book.xlsx", Workbook());

        var result = await Provider(source, "book.xlsx").Query(new DatasourceRequest { ObjectId = "book.xlsx#Nope" });

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("не найден");
    }

    [Fact]
    public async Task Query_HostPathWithDriveLetterIsNotSplit()
    {
        // ':' в Windows-пути не должен приниматься за разделитель листа — разделитель '#'.
        var source = new MemoryFileSource();
        source.Add(@"C:\data\sales.csv", SalesCsv);

        var result = await Provider(source, @"C:\data\sales.csv")
            .Query(new DatasourceRequest { ObjectId = @"C:\data\sales.csv" });

        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().HaveCount(3);
    }

    [Fact]
    public async Task Query_HasHeadersOffGivesGeneratedColumns()
    {
        var result = await Provider(SourceWithSales(),
                settings: new Dictionary<string, string> { [DatasourceSettings.HasHeaders] = "false" })
            .Query(new DatasourceRequest { ObjectId = "sales.csv" });

        result.Ok.Should().BeTrue(result.Message);
        result.Fields.Select(c => c.Name).Should().Equal("col1", "col2", "col3");
        result.Rows.Should().HaveCount(4);
    }

    [Fact]
    public async Task Modify_Refuses()
    {
        var result = await Provider(SourceWithSales()).Modify(new DatasourceRequest { ObjectId = "sales.csv", Query = "UPDATE" });

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("только для чтения");
    }

    [Fact]
    public void Profile_QueryAndBrowseOnly()
    {
        var profile = Provider(SourceWithSales()).Profile;

        profile.Kind.Should().Be(DatasourceKind.File);
        profile.Has(DatasourceFeature.Query).Should().BeTrue();
        profile.Has(DatasourceFeature.Browse).Should().BeTrue();
        profile.Has(DatasourceFeature.Write).Should().BeFalse();
        profile.Has(DatasourceFeature.Views).Should().BeFalse();
        profile.DefaultLanguage.Should().Be(DatasourceLanguage.Linq);
    }

    [Fact]
    public void AddDatasourceFile_RegistersProviderFactory()
    {
        var services = new ServiceCollection()
            .AddSingleton<IDatasourceFileSource, MemoryFileSource>()
            .AddDatasourceFile()
            .BuildServiceProvider();

        var factory = services.GetRequiredService<IEnumerable<IDatasourceProviderFactory>>().Single();

        factory.Profile.Kind.Should().Be(DatasourceKind.File);
        factory.Profile.Driver.Should().BeEmpty();
        factory.Create(new DatasourceConfig { Kind = DatasourceKind.File, Slug = Slug }).Should().BeOfType<FileDatasourceProvider>();
    }
}

/// <summary>Файлы источника в памяти: ссылки те же, что в медиа-хранилище или на хосте.</summary>
class MemoryFileSource : IDatasourceFileSource
{
    readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string reference, string content)
        => Add(reference, Encoding.UTF8.GetBytes(content));

    public void Add(string reference, byte[] content)
        => _files[reference] = content;

    public bool Exists(string reference) => _files.ContainsKey(reference);

    public Stream OpenRead(string reference)
        => Exists(reference)
            ? new MemoryStream(_files[reference])
            : throw new FileNotFoundException($"Файл \"{reference}\" не найден", reference);
}
