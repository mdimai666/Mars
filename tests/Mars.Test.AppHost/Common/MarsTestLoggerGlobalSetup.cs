using System.Runtime.CompilerServices;
using Mars.Server.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Mars.Integration.Tests.Common;

/// <summary>
/// Процесс-уровневая инициализация <see cref="MarsLogger"/> (паттерн NLog LogManager):
/// фабрика живёт весь тестовый процесс и не диспозится вместе с фикстурами-приложениями,
/// поэтому параллельные коллекции/хосты не перетирают и не «убивают» глобальный логгер.
/// </summary>
internal static class MarsTestLoggerGlobalSetup
{
    [ModuleInitializer]
    internal static void Init()
    {
        MarsLogger.Initialize(LoggerFactory.Create(builder => builder.AddSimpleConsole(o => o.SingleLine = true)));
    }
}
