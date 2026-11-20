using ClosedXML.Excel;
using FluentAssertions;
using Mars.Excel.Abstractions;
using Mars.Excel.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Excel.Tests;

/// <summary>
/// Регенерация отчётов через ClosedXML.Report (XLTemplate). Исторически генерация ломалась
/// между версиями библиотеки и в Release-сборке (шрифты/сателлитные сборки), поэтому прогон
/// выполняется в обоих конфигурациях: <c>dotnet test tests/Mars.Excel.Tests</c> и
/// <c>dotnet test tests/Mars.Excel.Tests -c Release</c>.
/// </summary>
public class ExcelReportGenerationTests
{
    static IExcelService CreateService() =>
        new ServiceCollection()
            .AddMarsExcel()
            .BuildServiceProvider()
            .GetRequiredService<IExcelService>();

    static string TemplatePath(string name) => Path.Combine(AppContext.BaseDirectory, "Templates", name);

    static IReadOnlyList<string> UsedCellStrings(MemoryStream stream)
    {
        stream.Position = 0;
        using var wb = new XLWorkbook(stream);
        return wb.Worksheets
            .SelectMany(ws => ws.RangeUsed()?.CellsUsed() ?? Enumerable.Empty<IXLCell>())
            .Select(c => c.GetFormattedString())
            .ToList();
    }

    //===================================== Синтетический шаблон

    static void CreateSyntheticTemplate(string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Report");
        ws.Cell("A1").Value = "{{Title}}";
        ws.Cell("A2").Value = "Наименование";
        ws.Cell("B2").Value = "Цена";
        ws.Cell("C2").Value = "Дата";
        ws.Cell("A3").Value = "{{item.Name}}";
        ws.Cell("B3").Value = "{{item.Price}}";
        ws.Cell("C3").Value = "{{item.SoldAt}}";
        wb.DefinedNames.Add("Items", ws.Range("A3:C3"));
        wb.SaveAs(path);
    }

    [Fact]
    public void BuildExcelReport_SyntheticTemplate_GeneratesTitleAndListRows()
    {
        var templateFile = Path.Combine(Path.GetTempPath(), $"mars-excel-test-{Guid.NewGuid():N}.xlsx");
        CreateSyntheticTemplate(templateFile);

        try
        {
            var viewModel = new SyntheticReportViewModel
            {
                Title = "Отчёт по товарам",
                Items =
                [
                    new ItemViewModel("Яблоко", 1.5m, new DateTime(2026, 11, 1)),
                    new ItemViewModel("Груша", 2.25m, new DateTime(2026, 11, 2)),
                ],
            };

            using var outStream = new MemoryStream();
            CreateService().BuildExcelReport(templateFile, viewModel, outStream);

            outStream.Length.Should().BeGreaterThan(0);

            var strings = UsedCellStrings(outStream);
            strings.Should().Contain("Отчёт по товарам");
            strings.Should().Contain("Яблоко");
            strings.Should().Contain("Груша");
            strings.Should().NotContain(s => s.Contains("{{"), "все плейсхолдеры должны быть заменены");
        }
        finally
        {
            File.Delete(templateFile);
        }
    }

    [Fact]
    public void BuildExcelReport_EmptyList_StillGeneratesWithoutPlaceholders()
    {
        var templateFile = Path.Combine(Path.GetTempPath(), $"mars-excel-test-{Guid.NewGuid():N}.xlsx");
        CreateSyntheticTemplate(templateFile);

        try
        {
            var viewModel = new SyntheticReportViewModel { Title = "Пустой отчёт", Items = [] };

            using var outStream = new MemoryStream();
            CreateService().BuildExcelReport(templateFile, viewModel, outStream);

            var strings = UsedCellStrings(outStream);
            strings.Should().Contain("Пустой отчёт");
            strings.Should().NotContain(s => s.Contains("{{"));
        }
        finally
        {
            File.Delete(templateFile);
        }
    }

    //===================================== Реальный шаблон feedback_list.xlsx

    [Fact]
    public void BuildExcelReport_RealFeedbackTemplate_GeneratesAllRows()
    {
        var templateFile = TemplatePath("feedback_list.xlsx");
        File.Exists(templateFile).Should().BeTrue($"прод-шаблон должен копироваться в вывод тестов: {templateFile}");

        var viewModel = new FeedbackReportViewModel
        {
            Title = "Письма",
            Feedbacks =
            [
                new FeedbackRow
                {
                    Title = "Первое письмо",
                    Email = "first@example.com",
                    Phone = "+7 900 000-00-01",
                    Content = "Текст первого письма",
                    CreatedAt = new DateTimeOffset(2026, 10, 1, 12, 30, 0, TimeSpan.FromHours(3)),
                },
                new FeedbackRow
                {
                    Title = "Второе письмо",
                    Email = "second@example.com",
                    Phone = "+7 900 000-00-02",
                    Content = "Текст второго письма",
                    CreatedAt = new DateTimeOffset(2026, 11, 5, 9, 15, 0, TimeSpan.FromHours(3)),
                },
            ],
        };

        using var outStream = new MemoryStream();
        CreateService().BuildExcelReport(templateFile, viewModel, outStream);

        outStream.Length.Should().BeGreaterThan(0);

        var strings = UsedCellStrings(outStream);
        strings.Should().Contain("Письма");
        strings.Should().Contain("Первое письмо");
        strings.Should().Contain("Второе письмо");
        strings.Should().Contain("first@example.com");
        strings.Should().Contain("second@example.com");
        strings.Should().Contain("+7 900 000-00-01");
        strings.Should().Contain("Текст первого письма");
        strings.Should().Contain(s => s.Contains("2026"), "дата {{item.CreatedAt.LocalDateTime}} должна отрендериться");
        strings.Should().NotContain(s => s.Contains("{{"), "все плейсхолдеры должны быть заменены");
    }

    //===================================== View models

    class SyntheticReportViewModel
    {
        public string Title { get; set; } = "";
        public IReadOnlyCollection<ItemViewModel> Items { get; set; } = [];
    }

    record ItemViewModel(string Name, decimal Price, DateTime SoldAt);

    /// <summary>Повторяет форму прод-DTO FeedbackService.FeedbacksExcelReportDto (Title + Feedbacks)
    /// и свойства FeedbackDetail, которые биндит шаблон feedback_list.xlsx.</summary>
    class FeedbackReportViewModel
    {
        public string Title { get; set; } = "Письма";
        public IReadOnlyCollection<FeedbackRow> Feedbacks { get; set; } = [];
    }

    class FeedbackRow
    {
        public string Title { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
    }
}
