using System.ComponentModel.DataAnnotations;

namespace Mars.Options.Models;

[Display(Name = "Настройки Favicon")]
public class FaviconOption
{
    [Display(Name = "ThemeColor", Description = "Цвет для адресной строки в мобильных браузерах")]
    public string ThemeColor { get; set; } = "#ffffff";
    public string BackgroundColor { get; set; } = "#ffffff";

    [Display(Name = "Исходное изображение для генерации Favicon")]
    public Guid FaviconSourceImageId { get; set; }

}

public class FaviconOptionGenaratedValues
{
    [Display(Name = "Сгенерированные мета-теги")]
    public string GeneratedMetaTags { get; set; } = string.Empty;
}
