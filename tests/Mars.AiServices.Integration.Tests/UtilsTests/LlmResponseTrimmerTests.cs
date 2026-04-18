using Mars.SemanticKernel.Host.Shared.Generators;

namespace Mars.AiServices.Integration.Tests.UtilsTests;

public class LlmResponseTrimmerTests
{
    [Fact]
    public void TestMarkdownJsonBlock()
    {
        var input = "```json\n{\"key\": \"value\"}\n```";
        var result = LlmResponseTrimmer.TrimResponse(input);
        Assert.Equal("{\"key\": \"value\"}", result);
    }

    [Fact]
    public void TestTextBeforeJson()
    {
        var input = "Вот ваш результат: {\"data\": 42}";
        var result = LlmResponseTrimmer.TrimResponse(input);
        Assert.Equal("{\"data\": 42}", result);
    }

    [Fact]
    public void TestMultipleBackticks()
    {
        var input = "````json\n{\"array\": [1, 2, 3]}\n````";
        var result = LlmResponseTrimmer.TrimResponse(input);
        Assert.Equal("{\"array\": [1, 2, 3]}", result);
    }

    [Fact]
    public void TestArrayResponse()
    {
        var input = "Result: [\"item1\", \"item2\"]";
        var result = LlmResponseTrimmer.TrimResponse(input);
        Assert.Equal("[\"item1\", \"item2\"]", result);
    }

    [Fact]
    public void TestExtractJson()
    {
        var input = "Some text {\"nested\": {\"value\": true}} more text";
        var result = LlmResponseTrimmer.ExtractJsonFromText(input);
        Assert.Equal("{\"nested\": {\"value\": true}}", result);
    }
}
