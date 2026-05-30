using Mars.Nodes.Core.Implements.Utils;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Mars.Nodes.Implements.Test.Utils;

public class FilePathGeneratorDateTests
{
    private static IFormFile CreateSubstituteFormFile(string fileName)
    {
        var fileSubstitute = Substitute.For<IFormFile>();
        fileSubstitute.FileName.Returns(fileName);
        return fileSubstitute;
    }

    [Fact]
    public void Generate_ShouldReplaceYearTokensCorrectly()
    {
        // Arrange
        string template = "archive/{yyyy}/{yy}/file.txt";
        IFormFile mockFile = CreateSubstituteFormFile("test.txt");
        DateTime now = DateTime.UtcNow;

        // Act
        string result = FilePathGenerator.Generate(template, mockFile, "field");

        // Assert
        string expectedYearFull = now.ToString("yyyy"); // Например, "2026"
        string expectedYearShort = now.ToString("yy");   // Например, "26"

        Assert.Contains($"archive/{expectedYearFull}/{expectedYearShort}/", result.Replace('\\', '/'));
    }

    [Fact]
    public void Generate_ShouldReplaceMonthTokensCorrectly()
    {
        // Arrange
        string template = "months/{MM}/{M}/file.txt";
        IFormFile mockFile = CreateSubstituteFormFile("test.txt");
        DateTime now = DateTime.UtcNow;

        // Act
        string result = FilePathGenerator.Generate(template, mockFile, "field");

        // Assert
        string expectedMonthLong = now.ToString("MM");    // С ведущим нулем, например "05"
        string expectedMonthShort = now.ToString("%M");   // Без нуля, например "5"

        Assert.Contains($"months/{expectedMonthLong}/{expectedMonthShort}/", result.Replace('\\', '/'));
    }

    [Fact]
    public void Generate_ShouldReplaceDayTokensCorrectly()
    {
        // Arrange
        string template = "days/{DD}/{D}/file.txt";
        IFormFile mockFile = CreateSubstituteFormFile("test.txt");
        DateTime now = DateTime.UtcNow;

        // Act
        string result = FilePathGenerator.Generate(template, mockFile, "field");

        // Assert
        string expectedDayLong = now.ToString("dd");    // С ведущим нулем, например "09"
        string expectedDayShort = now.ToString("%d");   // Без нуля, например "9"

        Assert.Contains($"days/{expectedDayLong}/{expectedDayShort}/", result.Replace('\\', '/'));
    }

    [Fact]
    public void Generate_ShouldReplaceHourTokensCorrectly()
    {
        // Arrange
        string template = "hours/{HH}/{H}/file.txt";
        IFormFile mockFile = CreateSubstituteFormFile("test.txt");
        DateTime now = DateTime.UtcNow;

        // Act
        string result = FilePathGenerator.Generate(template, mockFile, "field");

        // Assert
        string expectedHourLong = now.ToString("HH");   // С ведущим нулем, 24ч формат
        string expectedHourShort = now.ToString("%H");  // Без нуля, 24ч формат

        Assert.Contains($"hours/{expectedHourLong}/{expectedHourShort}/", result.Replace('\\', '/'));
    }

    [Fact]
    public void Generate_ShouldReplaceMinutesAndSecondsTokensCorrectly()
    {
        // Arrange
        string template = "time/{mm}/{ss}/file.txt";
        IFormFile mockFile = CreateSubstituteFormFile("test.txt");

        // Берем время прямо перед вызовом, учитывая возможный сдвиг в 1 секунду
        DateTime beforeAct = DateTime.UtcNow;

        // Act
        string result = FilePathGenerator.Generate(template, mockFile, "field");

        // Assert
        DateTime afterAct = DateTime.UtcNow;

        // Так как секунды бегут быстро, проверяем соответствие либо времени ДО, либо времени ПОСЛЕ вызова
        string expectedMinBefore = beforeAct.ToString("mm");
        string expectedSecBefore = beforeAct.ToString("ss");
        string expectedMinAfter = afterAct.ToString("mm");
        string expectedSecAfter = afterAct.ToString("ss");

        string normalizedResult = result.Replace('\\', '/');

        bool matchBefore = normalizedResult.Contains($"time/{expectedMinBefore}/{expectedSecBefore}/");
        bool matchAfter = normalizedResult.Contains($"time/{expectedMinAfter}/{expectedSecAfter}/");

        Assert.True(matchBefore || matchAfter, "Токены минут или секунд не совпали с текущим временем системы.");
    }

    [Fact]
    public void Generate_ShouldHandleComplexDateTimeTemplateCombined()
    {
        // Arrange
        string template = "{yyyy}-{MM}-{DD}_{HH}h{mm}m/file.txt";
        IFormFile mockFile = CreateSubstituteFormFile("test.txt");
        DateTime now = DateTime.UtcNow;

        // Act
        string result = FilePathGenerator.Generate(template, mockFile, "field");

        // Assert
        // Собираем точную маску и проверяем её наличие в начале пути
        string expectedDatePart = now.ToString("yyyy-MM-dd_HH"); // Безопасная часть до минут
        Assert.Contains(expectedDatePart, result);
    }
}
