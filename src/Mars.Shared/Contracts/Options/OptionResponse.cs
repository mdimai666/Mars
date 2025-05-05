using System.ComponentModel.DataAnnotations;

namespace Mars.Shared.Contracts.Options;

public sealed record OptionResponse<T>
{
    [Display(Name = "Ключ название")]
    public required string Key { get; init; }

    [Display(Name = "Тип")]
    public required string Type { get; init; } 

    [Display(Name = "Значение")]
    public required T Value { get; init; }
}

public record OptionSummaryResponse
{
    public required string Key { get; init; }
    public required string Type { get; init; }
    public required string Value { get; init; }
}

public record OptionDetailResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required string Key { get; init; }
    public required string Type { get; init; }
    public required string Value { get; init; }
}
