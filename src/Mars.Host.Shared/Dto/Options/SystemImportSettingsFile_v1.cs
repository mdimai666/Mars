using System.ComponentModel.DataAnnotations;

namespace Mars.Host.Shared.Dto.Options;

public class SystemImportSettingsFile_v1
{
    //public SysOptions SysOptions { get; set; }
    //public SmtpSettingsModel SmtpSettings { get; set; }
    //public IEnumerable<Role> Roles { get; set; }
    //public IEnumerable<ContactPerson> ContactPersons { get; set; }
}

public class SystemImportSettingsFile_v1_select
{
    [Display(Name = "Системные настройки")]
    public bool SysOptions { get; set; }
    [Display(Name = "Настройки почты")]
    public bool SmtpSettings { get; set; }
    [Display(Name = "Роли")]
    public bool Roles { get; set; }
    [Display(Name = "Вопросы и ответы")]
    public bool FaqPosts { get; set; }
    [Display(Name = "Контакты")]
    public bool ContactPersons { get; set; }

}
