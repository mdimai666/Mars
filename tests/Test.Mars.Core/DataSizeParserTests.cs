using Mars.Core.Extensions;

namespace Test.Mars.Core;

public class DataSizeParserTests
{
    // ==========================================
    // ПОЗИТИВНЫЕ ТЕСТЫ (SUCCESS SCENARIOS)
    // ==========================================

    [Theory]
    [InlineData("10b", 10)]
    [InlineData("500", 500)] // Без суффикса считаем как байты
    [InlineData("1kb", 1024)]
    [InlineData("10mb", 10_485_760)]
    [InlineData("2 gb", 2_147_483_648)] // С пробелом
    [InlineData("1.5GB", 1_610_612_736)] // Дробное число через точку
    [InlineData("1,5GB", 1_610_612_736)] // Дrobное число через запятую
    [InlineData("100MB", 104_857_600)] // Верхний регистр
    public void ParseToBytes_ValidString_ReturnsCorrectBytes(string input, long expectedBytes)
    {
        // Arrange & Act (для Theory Arrange минимален, так как входные данные идут из параметров)
        long actualBytes = DataSizeParser.ParseToBytes(input);

        // Assert
        Assert.Equal(expectedBytes, actualBytes);
    }

    // ==========================================
    // НЕГАТИВНЫЕ ТЕСТЫ (FAILURE SCENARIOS)
    // ==========================================

    [Fact]
    public void ParseToBytes_StringIsEmptyOrNull_ThrowsArgumentException()
    {
        // Arrange
        string invalidInput = "   ";

        // Act
        Action act = () => DataSizeParser.ParseToBytes(invalidInput);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData("10pb")] // Неизвестная единица (Петабайт)
    [InlineData("10xyz")] // Мусор в суффиксе
    public void ParseToBytes_UnknownUnit_ThrowsFormatException(string invalidInput)
    {
        // Arrange & Act
        Action act = () => DataSizeParser.ParseToBytes(invalidInput);

        // Assert
        var exception = Assert.Throws<FormatException>(act);
    }

    [Theory]
    [InlineData("abc")] // Совсем не число
    [InlineData("10.5.5mb")] // Неправильный формат числа
    public void ParseToBytes_InvalidNumberFormat_ThrowsFormatException(string invalidInput)
    {
        // Arrange & Act
        Action act = () => DataSizeParser.ParseToBytes(invalidInput);

        // Assert
        var exception = Assert.Throws<FormatException>(act);
    }
}
