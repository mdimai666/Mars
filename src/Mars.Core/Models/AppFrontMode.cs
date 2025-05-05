namespace Mars.Core.Models;

public enum AppFrontMode : int
{
    None = 0,
    HandlebarsTemplate = 1,
    HandlebarsTemplateStatic = 10,
    ServeStaticBlazor = 2,
    BlazorPrerender = 3,
}
