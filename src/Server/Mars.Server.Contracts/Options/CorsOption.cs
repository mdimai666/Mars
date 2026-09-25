using System.ComponentModel.DataAnnotations;

namespace Mars.Server.Contracts.Options;

[Display(Name = "CORS")]
public class CorsOption
{
    [Display(Name = "Разрешённые origins", Description = "Кросс-доменные фронты, которым разрешены запросы с куками (https://site.com). Пусто — только same-origin")]
    public List<string> AllowedOrigins { get; set; } = [];
}
