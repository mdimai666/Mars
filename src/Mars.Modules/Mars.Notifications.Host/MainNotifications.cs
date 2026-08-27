using Mars.Notifications.Abstractions;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Notifications.Host;

public static class MainNotifications
{
    public static IServiceCollection AddMarsNotifications(this IServiceCollection services)
    {
        services.AddTransient<IMarsEmailSender, EmailSender>();
        services.AddTransient<IEmailSender, EmailSender>();
        services.AddTransient<ISmsSender, SmsSender>();
        services.AddTransient<INotifyService, NotifyService>();
        return services;
    }
}
