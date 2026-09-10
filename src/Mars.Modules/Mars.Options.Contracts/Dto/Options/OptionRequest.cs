using System.ComponentModel.DataAnnotations;
using Mars.Contracts.Common;

namespace Mars.Options.Contracts.Dto.Options;

public class OptionRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 3)]
    public required string Key { get; set; }
}

public record ListOptionQueryRequest : BasicListQueryRequest
{

}

public record TableOptionQueryRequest : BasicTableQueryRequest
{

}
