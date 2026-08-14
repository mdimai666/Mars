using System.Reflection;
using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Shared.Definitions;
using Mars.PxBlocks.Shared.Toolbox;

namespace Mars.PxBlocks.Host.Shared.Services;

/// <summary>
/// Серверный реестр блоков: определения (PxBlockSet → JSON для редактора) +
/// реализации исполнения (IPxBlockImplement по TypeId). Наполняется при старте
/// (UsePxBlocks/RegisterAssembly), читается запросами.
/// </summary>
public interface IPxBlockCatalog
{
    /// <summary>Все зарегистрированные определения блоков.</summary>
    IReadOnlyList<PxBlockDefinition> Definitions { get; }

    /// <summary>Toolbox: дефолт (PxDefaultToolbox) + зарегистрированные доменные категории.</summary>
    PxToolbox Toolbox { get; }

    /// <summary>Локатор реализаций исполнения — им пользуются PxParser и PxInterpreter.</summary>
    PxBlockImplementsLocator Implements { get; }

    /// <summary>Регистрация набора определений (PxBlockSet).</summary>
    void RegisterSet(PxBlockSet set);

    /// <summary>Доменная категория в toolbox (встаёт перед «Переменные»/«Функции»).</summary>
    void RegisterToolboxCategory(PxToolboxCategory category);

    /// <summary>
    /// Сканирует сборку: подклассы PxBlockSet (определения) и реализации
    /// IPxBlockImplement (исполнение) — паттерн RegisterAssembly из Mars.Nodes.
    /// </summary>
    void RegisterAssembly(Assembly assembly);
}
