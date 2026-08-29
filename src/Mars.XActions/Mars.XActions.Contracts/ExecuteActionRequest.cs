namespace Mars.Contracts.Common;

public class ExecuteActionRequest
{
    public required string ActionId { get; set; }
    public required Dictionary<string, string> Arguments { get; set; }
}
