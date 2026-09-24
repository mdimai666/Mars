using System.ComponentModel.DataAnnotations;

namespace Mars.Identity.Contracts.Options;

[Display(Name = "Настройки защиты авторизации")]
public class AuthProtectionOption
{
    public const string RateLimitPolicyName = "auth";

    [Display(Name = "Включить защиту")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "Окно лимитера (секунды)")]
    [Range(1, 3600)]
    public int RateWindowSeconds { get; set; } = 60;

    [Display(Name = "Максимум запросов авторизации на IP за окно")]
    [Range(1, 10000)]
    public int RateMaxRequestsPerWindow { get; set; } = 10;

    [Display(Name = "Неудачных попыток до блокировки")]
    [Range(1, 100)]
    public int LockoutMaxFailedAccessAttempts { get; set; } = 5;

    [Display(Name = "Длительность блокировки (минуты)")]
    [Range(1, 10080)]
    public int LockoutDefaultLockoutTimeSpanMinutes { get; set; } = 5;
}
