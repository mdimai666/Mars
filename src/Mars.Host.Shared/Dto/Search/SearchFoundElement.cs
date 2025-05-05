using System;
using Mars.Host.Shared.Dto.NavMenus;
using Mars.Shared.Contracts.Search;
using Mars.Shared.Contracts.XActions;

namespace Mars.Host.Shared.Dto.Search;

/// <summary>
/// <see cref="SearchFoundElementResponse"/>
/// </summary>
public record SearchFoundElement
{
    public required string Title { get; init; }
    public required string Key { get; init; }
    public string? Description { get; init; }
    public string? Url { get; init; }
    public required float Relevant { get; init; } = 1;

    public required FoundElementType Type { get; init; }

    public Dictionary<string, string> Data { get; init; } = [];

    public static SearchFoundElement CreateUrl(string title, string url)
        => new() { Title = title, Url = url, Key = url, Type = FoundElementType.Url, Relevant = 1 };

    public static SearchFoundElement CreateAction(XActionCommand action)
        => new()
        {
            Title = action.Label,
            Key = action.Id,
            Description = action.FrontContextId?.FirstOrDefault(),
            Url = action.LinkValue,
            Type = FoundElementType.XAction,
            Relevant = 10
        };

    public static SearchFoundElement CreatePage(NavMenuItemDto nav)
        => new()
        {
            Title = nav.Title,
            Key = nav.Id.ToString(),
            Url = nav.Url,
            Type = FoundElementType.Page,
            Relevant = 5
        };

    public static SearchFoundElement CreateRecord(string title, string url, Guid id, string typeName, string? desc)
        => new()
        {
            Title = title,
            Key = $"/{typeName}/{id}",
            Url = url,
            Type = FoundElementType.Record,
            Description = desc,
            Data = new() { ["Type"] = typeName, ["Id"] = id.ToString() },
            Relevant = 2
        };

}

