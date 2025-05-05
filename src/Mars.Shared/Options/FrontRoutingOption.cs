using System.ComponentModel.DataAnnotations;

namespace Mars.Shared.Options;

public class FrontRoutingOption
{
    [Display(Name = "Index page")]
    public Guid IndexPageId { get; set; }

    [Display(Name = "Fallback page")]
    public Guid FallbackPageId { get; set; }
}
