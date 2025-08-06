using Mars.Plugin.Abstractions;

namespace Mars.Plugin.Dto;

public record PluginData(bool hasConfigureWebApplicationBuilder,
                      bool hasConfigureWebApplication,
                      PluginSettings Settings,
                      WebApplicationPlugin Plugin, PluginInfo Info);
