using FluentAssertions;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Front.Workspaces.Objects;
using Mars.Datasource.Providers.File;

namespace Mars.Datasource.Tests.FileProviders;

/// <summary>
/// Примеры условий из меню «примеры» — рабочий текст запроса: каждый обязан разбираться
/// Dynamic LINQ и отбирать строки на файле с колонками, которые использованы в заготовках.
/// </summary>
public class FileQueryExamplesTests
{
    const string Csv =
        "name,age,description,created,active,price,category\n" +
        "ann,35,first record,2021-05-01,true,150,tools\n" +
        "bob,24,error occurred,2019-01-01,false,50,toys\n";

    public static TheoryData<FileQueryExample> Examples()
    {
        var data = new TheoryData<FileQueryExample>();

        foreach (var example in FileQueryExamples.All) data.Add(example);

        return data;
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public async Task Example_ParsesAndSelectsRows(FileQueryExample example)
    {
        var source = new MemoryFileSource();
        source.Add("sales.csv", Csv);

        var provider = new FileDatasourceProvider(new DatasourceConfig
        {
            Kind = DatasourceKind.File,
            Slug = "files",
            Title = "Files",
            Settings = new Dictionary<string, string> { [DatasourceSettings.Files] = "sales.csv" },
        }, source);

        var result = await provider.Query(new DatasourceRequest { ObjectId = "sales.csv", Query = example.Text });

        result.Ok.Should().BeTrue($"{example.Name}: {result.Message}");
        result.Rows.Should().NotBeEmpty(example.Name);
    }
}
