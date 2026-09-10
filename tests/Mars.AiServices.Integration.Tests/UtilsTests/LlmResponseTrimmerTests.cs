using Mars.SemanticKernel.Abstractions.Generators;

namespace Mars.AiServices.Integration.Tests.UtilsTests;

public class LlmResponseTrimmerTests
{
    [Fact]
    public void TrimResponse_JsonInMarkdownBlock_ExtractsJson()
    {
        var input = "```json\n{\"key\": \"value\"}\n```";
        var result = LlmResponseTrimmer.TrimResponse(input);
        Assert.Equal("{\"key\": \"value\"}", result);
    }

    [Fact]
    public void TrimResponse_TextBeforeJson_ExtractsJson()
    {
        var input = "Вот ваш результат: {\"data\": 42}";
        var result = LlmResponseTrimmer.TrimResponse(input);
        Assert.Equal("{\"data\": 42}", result);
    }

    [Fact]
    public void TrimResponse_JsonInFourBacktickBlock_ExtractsJson()
    {
        var input = "````json\n{\"array\": [1, 2, 3]}\n````";
        var result = LlmResponseTrimmer.TrimResponse(input);
        Assert.Equal("{\"array\": [1, 2, 3]}", result);
    }

    [Fact]
    public void TrimResponse_TextBeforeJsonArray_ExtractsArray()
    {
        var input = "Result: [\"item1\", \"item2\"]";
        var result = LlmResponseTrimmer.TrimResponse(input);
        Assert.Equal("[\"item1\", \"item2\"]", result);
    }

    [Fact]
    public void ExtractJsonFromText_JsonSurroundedByText_ExtractsObject()
    {
        var input = "Some text {\"nested\": {\"value\": true}} more text";
        var result = LlmResponseTrimmer.ExtractJsonFromText(input);
        Assert.Equal("{\"nested\": {\"value\": true}}", result);
    }
}
