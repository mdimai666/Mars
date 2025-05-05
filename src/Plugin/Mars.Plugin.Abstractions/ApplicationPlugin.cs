using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Plugin.Abstractions;

public class WebApplicationPlugin
{
    public virtual void ConfigureWebApplicationBuilder(WebApplicationBuilder builder, PluginSettings settings) { }
    public virtual void ConfigureWebApplication(WebApplication app, PluginSettings settings) { }
}

public class PluginSettings
{
    public string ContentRootPath { get; init; } = default!;
}

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public sealed class WebApplicationPluginAttribute : Attribute
{
    public WebApplicationPluginAttribute(Type pluginType)
    {
        if (!(pluginType.IsClass && !pluginType.IsAbstract && typeof(WebApplicationPlugin).IsAssignableFrom(pluginType)))
        {
            throw new NotSupportedException($"{pluginType} is not a supported {nameof(WebApplicationPlugin)}");
        }

        PluginType = pluginType;
    }

    public Type PluginType { get; }
}