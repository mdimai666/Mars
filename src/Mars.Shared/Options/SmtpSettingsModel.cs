using System.ComponentModel.DataAnnotations;
using Mars.Shared.Resources;

namespace Mars.Shared.Options;

public class SmtpSettingsModel
{
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Почтовый сервер")]
    public string Host { get; set; } = "";

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Порт")]
    public int Port { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Логин")]
    public string SmtpUser { get; set; } = "";

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Пароль")]
    public string SmtpPassword { get; set; } = "";

    [Display(Name = "Защищенное")]
    public bool Secured { get; set; } = true;


    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "От имени")]
    public string FromName { get; set; } = "";

    [Display(Name = "Тестовый сервер. Не отправлять уведомления")]
    public bool IsTestServer { get; set; }

}
