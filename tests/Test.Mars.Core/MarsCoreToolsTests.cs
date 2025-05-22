using Mars.Core.Features;

namespace Test.Mars.Core;

public class MarsCoreToolsTests
{
    [Fact]
    public void TestTranslateToPostSlug()
    {
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["test"] = "test",
            ["тест"] = "test",
            ["тест2 "] = "test2",
            [" тест3 "] = "test3",
            [" тест4 "] = "test4",
            ["Филиал «Интересной компании \"Экстра\"»"] = "filial_interesnoy_kompanii_ekstra",
            ["абв123-=_!?abc!@#$%^&*()[]{};.,"] = "abv123-_abc.",
        };

        foreach (var item in values)
        {
            string result = TextTool.TranslateToPostSlug(item.Key);
            Assert.Equal(item.Value, result);
        }

    }
}
