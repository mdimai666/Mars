using System.Text.RegularExpressions;
using Mars.Nodes.Core.Implements.Utils;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Mars.Nodes.Implements.Test.Utils;

public class FilePathGeneratorTests
{
    // Вспомогательный метод для создания заглушки файла через NSubstitute
    private static IFormFile CreateSubstituteFormFile(string fileName)
    {
        // Создаем субститут (заглушку) для интерфейса IFormFile
        var fileSubstitute = Substitute.For<IFormFile>();

        // Настраиваем возврат имени файла при обращении к свойству FileName
        fileSubstitute.FileName.Returns(fileName);

        return fileSubstitute;
    }

    [Fact]
    public void Generate_ShouldReplaceFileTokensCorrectly_WhenValidTemplateProvided()
    {
        // Arrange (Подготовка)
        string template = "uploads/{field_name}/{file_name_only}{file_ext}";
        string fieldName = "avatar";
        string originalFileName = "user_photo.PNG";
        IFormFile mockFile = CreateSubstituteFormFile(originalFileName);

        // Act (Действие)
        string result = FilePathGenerator.Generate(template, mockFile, fieldName);

        // Assert (Проверка)
        string expectedPath = Path.Combine("uploads", "avatar", "user_photo.png");
        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void Generate_ShouldReplaceDateTimeTokensWithCurrentTime()
    {
        // Arrange
        string template = "{yyyy}/{MM}/{DD}/{HH}{mm}";
        string fieldName = "docs";
        IFormFile mockFile = CreateSubstituteFormFile("report.pdf");
        DateTime now = DateTime.UtcNow;

        // Act
        string result = FilePathGenerator.Generate(template, mockFile, fieldName);

        // Assert
        string expectedYear = now.ToString("yyyy");
        string expectedMonth = now.ToString("MM");
        string expectedDay = now.ToString("dd");

        Assert.Contains(expectedYear, result);
        Assert.Contains(expectedMonth, result);
        Assert.Contains(expectedDay, result);
    }

    [Fact]
    public void Generate_ShouldReplaceGuidTokenWithValid32CharHash()
    {
        // Arrange
        string template = "files/{guid}.dat";
        IFormFile mockFile = CreateSubstituteFormFile("test.txt");

        // Act
        string result = FilePathGenerator.Generate(template, mockFile, "field");

        // Assert
        string fileNameWithExt = Path.GetFileName(result);
        string fileNameOnly = Path.GetFileNameWithoutExtension(fileNameWithExt);

        Assert.True(Regex.IsMatch(fileNameOnly, "^[a-fA-F0-9]{32}$"),
            $"Имя файла '{fileNameOnly}' не является валидным GUID в формате N.");
    }

    [Fact]
    public void Generate_ShouldReplaceUniqueSuffixToken()
    {
        // Arrange
        string template = "files/{unique_suffix}.dat";
        IFormFile mockFile = CreateSubstituteFormFile("test.txt");

        // Act
        string result = FilePathGenerator.Generate(template, mockFile, "field");

        // Assert
        string fileNameWithExt = Path.GetFileName(result);
        string fileNameOnly = Path.GetFileNameWithoutExtension(fileNameWithExt);

        Assert.True(Regex.IsMatch(fileNameOnly, "^\\d{8}_[a-fA-F0-9]{8}$"),
            $"Имя файла '{fileNameOnly}' не является валидным TextTool.GenerateUniqueSuffix.");
    }

    [Fact]
    public void Generate_ShouldCleanForbiddenCharacters_ToPreventPathTraversal()
    {
        // Arrange
        string template = "storage/{field_name}/{file_name}";
        string dangerousFieldName = "user/../admin";
        string dangerousFileName = "..\\..\\etc/passwd.jpg";
        IFormFile mockFile = CreateSubstituteFormFile(dangerousFileName);

        // Act
        string result = FilePathGenerator.Generate(template, mockFile, dangerousFieldName);

        // Assert
        Assert.DoesNotContain("..", result);
        Assert.DoesNotContain("/", result.Substring(8)); // Игнорируем базовую папку 'storage/'
        Assert.Contains("user___admin", result);
        Assert.Contains("___etc_passwd.jpg".Trim("_"), result);
    }

    [Fact]
    public void Generate_ShouldThrowArgumentNullException_WhenTemplateIsEmpty()
    {
        // Arrange
        string nullTemplate = null!;
        IFormFile mockFile = CreateSubstituteFormFile("photo.jpg");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FilePathGenerator.Generate(nullTemplate, mockFile, "field"));
    }

    [Fact]
    public void Generate_ShouldNormalizePathSeparatorsForCurrentOperatingSystem()
    {
        // Arrange
        string template = "root/sub\\folder/file.txt";
        IFormFile mockFile = CreateSubstituteFormFile("dummy.txt");

        // Act
        string result = FilePathGenerator.Generate(template, mockFile, "field");

        // Assert
        if (Path.DirectorySeparatorChar == '/')
        {
            Assert.DoesNotContain("\\", result);
        }
        else if (Path.DirectorySeparatorChar == '\\')
        {
            Assert.DoesNotContain("/", result);
        }
    }
}
