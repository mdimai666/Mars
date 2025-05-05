using Mars.Nodes.Core.Implements.Nodes;

namespace Mars.Nodes.Implements.Test.Nodes;

public class JsonNodeTests
{
    [Fact]
    public void ToJsonString_ObjectToString_Success()
    {
        //Arrange
        _ = nameof(JsonNodeImpl.ToJsonString);
        var val = new
        {
            Name = "Dima"
        };

        string expect = @"{""name"":""Dima""}";

        //Act
        //Assert

        //Pure object
        string json = JsonNodeImpl.ToJsonString(val, false);

        Assert.Equal(expect, json);


        //Newtonsoft
        var newtonObject = Newtonsoft.Json.JsonConvert.DeserializeObject(expect)!;

        string jsonFromNewtonObject = JsonNodeImpl.ToJsonString(newtonObject, false);

        Assert.Equal(expect, jsonFromNewtonObject);

        //System.Text
        var systemTextObject = System.Text.Json.JsonSerializer.Deserialize<object>(expect)!;

        string jsonFromSystemText = JsonNodeImpl.ToJsonString(systemTextObject, false);

        Assert.Equal(expect, jsonFromSystemText);
    }

    [Fact]
    public void ParseString_StringToObject_Success()
    {
        //Arrange
        _ = nameof(JsonNodeImpl.ParseString);
        var val = new
        {
            Name = "Dima"
        };

        string json = @"{""name"":""Dima""}";

        //Act
        var deserialized = JsonNodeImpl.ParseString(json)!;

        //Assert
        Assert.Equal(val.Name, deserialized["name"].ToString());

    }
}
