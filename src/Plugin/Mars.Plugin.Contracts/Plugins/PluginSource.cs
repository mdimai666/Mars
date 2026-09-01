namespace Mars.Plugin.Contracts.Plugins;

public enum PluginSource
{
    Unknown = 0,
    /// <summary>Принудительный плагин из конфигурации инстанса — не удаляется и не отключается из админки.</summary>
    Config = 1,
    Zip = 2,
    NuGet = 3,
}
