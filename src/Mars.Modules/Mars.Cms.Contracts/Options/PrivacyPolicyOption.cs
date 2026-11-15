using System.ComponentModel.DataAnnotations;

namespace Mars.Cms.Contracts.Options;

/// <summary>
/// Страница политики конфиденциальности (аналог Settings → Privacy в WordPress):
/// публичная /privacy рендерит выбранный пост без авторизации.
/// </summary>
[Display(Name = "Политика")]
public class PrivacyPolicyOption
{
    [Display(Name = "Страница политики", Description = "Пост, который показывается на публичной странице /privacy. Пока не выбран — страница показывает заглушку.")]
    public Guid? PageId { get; set; }
}
