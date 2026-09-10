using Microsoft.AspNetCore.Builder;

namespace Mars.Plugin.Abstractions;

public class MarsPlugin
{
    public virtual void ConfigureWebApplicationBuilder(WebApplicationBuilder builder, PluginSettings settings) { }
    public virtual void ConfigureWebApplication(WebApplication app, PluginSettings settings) { }
}

public class PluginSettings
{
    public string ContentRootPath { get; init; } = default!;
}

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public class MarsPluginAttribute : Attribute
{
    public MarsPluginAttribute(Type pluginType)
    {
        if (!(pluginType.IsClass && !pluginType.IsAbstract && typeof(MarsPlugin).IsAssignableFrom(pluginType)))
        {
            throw new NotSupportedException($"{pluginType} is not a supported {nameof(MarsPlugin)}");
        }

        PluginType = pluginType;
    }

    public Type PluginType { get; }
}
