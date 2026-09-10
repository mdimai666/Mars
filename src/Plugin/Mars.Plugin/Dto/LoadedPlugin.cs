using Mars.Plugin.Abstractions;

namespace Mars.Plugin.Dto;

public record LoadedPlugin(bool hasConfigureWebApplicationBuilder,
                      bool hasConfigureWebApplication,
                      PluginSettings Settings,
                      MarsPlugin Plugin, PluginInfo Info);
