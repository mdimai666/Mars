using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Mars.Admin.Builder.StyleDesignerViews;

[RequiresUnreferencedCode("Necessary because of RangeAttribute usage")]
public class StyleSampleModel
{
    [Required]
    [MinLength(3, ErrorMessage = "Name is too short!")]
    [StringLength(16, ErrorMessage = "Name too long (16 character limit).")]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = "A category is required")]
    public string? Category { get; set; }

    [Range(1, 100000, ErrorMessage = "Amount invalid (1-100000).")]
    public int Amount { get; set; }

    [Required]
    [Range(typeof(bool), "true", "true",
        ErrorMessage = "Approval is required.")]
    public bool Approved { get; set; }

    [Required]
    public DateTime? Date { get; set; }

    public bool Enabled { get; set; }
}
