using System.Text.Json;
using EditorJsBlazored.Blocks;
using EditorJsBlazored.Core;

namespace Test.EditorJsBlazored;

public class BlockEditorContentTests
{
    [Fact]
    public void ToJson_ProperyCaseMustLower_ExpecetValueNameLower()
    {
        //Arrange
        var content = new EditorJsContent
        {
            Blocks = [
                new (){ Type = "unknown", Data = new { Value = "333" } }
            ]
        };

        //Act
        var json = content.ToJson();

        //Assert
        Assert.Contains(@"""value""", json);
    }

    [Fact]
    public void Content_SaveUnknownBlockData_UnknownDataMustBePreserved()
    {
        //Arrange
        var block = new BlockParagraph { Text = "123" };
        var content = new EditorJsContent
        {
            Blocks = [
                new () { Type = "paragraph", Data = block },
                new () { Type = "unknown", Data = new { Value = "333" } }
            ]
        };

        var json = content.ToJson();

        //Act
        var deserialized = EditorJsContent.FromJson(json);

        //Assert
        var unknownValue = JsonSerializer.SerializeToNode(deserialized.Blocks[1].Data)!["value"]!.GetValue<string>();
        Assert.Equal("333", unknownValue);
    }

    [Fact]
    public void EditorJsContent_ModifiedTimestamp_ShouldToDateTime()
    {
        //Arrange
        //1739976214843 - 19.02.2025 23:43:40
        var content = new EditorJsContent
        {
            Time = 1739976214843
        };

        //Act
        var dateTime = content.TimeAsDateTime;

        //Assert
        Assert.Equal("19.02.2025 23:43:34", dateTime.LocalDateTime.ToString("dd.MM.yyyy HH:mm:ss"));
    }

}
