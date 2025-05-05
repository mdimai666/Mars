using System.ComponentModel.DataAnnotations;
using Mars.Shared.Resources;

namespace Mars.Shared.Options;

public class TestMailMessage
{
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Кому. Почта.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Тема")]
    public string Subject { get; set; } = "";
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Сообщение")]
    public string Message { get; set; } = "";

}
