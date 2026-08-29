using System.Reflection;
using Mars.Notifications.Abstractions;
using Mars.Options.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Notifications.Host;

public static class MainNotifications
{
    public static IServiceCollection AddMarsNotifications(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        services.AddTransient<IMarsEmailSender, EmailSender>();
        services.AddTransient<IEmailSender, EmailSender>();
        services.AddTransient<ISmsSender, SmsSender>();
        services.AddTransient<INotifyService, NotifyService>();
        return services;
    }

    public static IServiceProvider UseMarsNotifications(this IServiceProvider services)
    {
        var optionService = services.GetRequiredService<IOptionService>();
        optionService.RegisterOption<SmtpSettingsModel>();
        optionService.GetOption<SmtpSettingsModel>();
        return services;
    }
}
