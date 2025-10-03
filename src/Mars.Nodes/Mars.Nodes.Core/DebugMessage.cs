using System.Text.Json;
using Mars.Core.Models;

namespace Mars.Nodes.Core;

public class DebugMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? NodeId { get; init; }
    //public string Id { get; init; } = "";
    //public string Topic { get; init; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public string Message { get; init; } = default!;
    public string? Json { get; init; }

    public Mars.Core.Models.MessageIntent Level { get; init; }

    public static DebugMessage Test()
        => new()
        {
            //Id = Guid.NewGuid().ToString(),
            //Topic = "topic",
            Json = JsonSerializer.Serialize(new DebugMessage())
        };

    public static DebugMessage ConsoleMessage(string text, MessageIntent Level = MessageIntent.Info)
        => new() { Message = text, Level = Level };

    public static DebugMessage NodeMessage(string nodeId, string text, MessageIntent Level = MessageIntent.Info)
        => new() { Message = text, Level = Level, NodeId = nodeId };

    public static DebugMessage NodeWarnMessage(string nodeId, string text)
        => new() { Message = text, Level = MessageIntent.Warning, NodeId = nodeId };

    public static DebugMessage NodeErrorMessage(string nodeId, string text)
        => new() { Message = text, Level = MessageIntent.Error, NodeId = nodeId };

    public static DebugMessage NodeException(string nodeId, Exception ex)
        => new() { Message = ex.Message, Level = MessageIntent.Error, NodeId = nodeId };

}

class DebugMessagePayload
{
    public string message { get; init; } = "message";
    public int counter { get; init; } = 555;

}
