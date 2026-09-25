using System.ComponentModel.DataAnnotations;

namespace Mars.Identity.Contracts.Options;

[Display(Name = "Настройки пасскеев")]
public class PasskeyOption
{
    [Display(Name = "Разрешить пасскеи")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "Максимум пасскеев на пользователя")]
    [Range(1, 100)]
    public int PasskeysMaxPerUser { get; set; } = 10;
}
