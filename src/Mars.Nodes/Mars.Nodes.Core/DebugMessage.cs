using System.Text.Json;

namespace Mars.Nodes.Core;

public class DebugMessage
{
    public string id { get; set; } = "";
    public string topic { get; set; } = "";
    public DateTime date { get; set; } = DateTime.Now;
    public string message { get; set; } = default!;
    public string json { get; set; } = default!;

    public Mars.Core.Models.MessageIntent Level { get; set; }

    public static DebugMessage Test()
    {
        return new DebugMessage
        {
            id = Guid.NewGuid().ToString(),
            topic = "topic",
            json = JsonSerializer.Serialize(new DebugMessage())
        };

    }
}

class DebugMessagePayload
{
    public string message { get; set; } = "message";
    public int counter { get; set; } = 555;

}
