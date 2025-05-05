using System.ComponentModel.DataAnnotations;

namespace Mars.Options.Models;

[Display(Name = "SEO")]
public class SEOOption
{
    [Display(Name = "robots.txt", Description = "Robots.txt — это текстовый файл, который содержит параметры индексирования сайта для роботов поисковых систем. В robots.txt можно ограничить индексирование роботами страниц сайта, что может снизить нагрузку на сайт и ускорить его работу.")]
    [MaxLength(500 * 1024)]
    public string RobotsTxt { get; set; } = "User-agent: * \r\nDisallow: /dev/\r\nDisallow: /api/\r\nDisallow: /admin/";
}