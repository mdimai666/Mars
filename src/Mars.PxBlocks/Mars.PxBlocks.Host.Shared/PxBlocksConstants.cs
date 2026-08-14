namespace Mars.PxBlocks.Host.Shared;

/// <summary>Константы серверного исполнения PxBlocks (маршруты, группы, имена методов хаба).</summary>
public static class PxBlocksConstants
{
    /// <summary>Маршрут SignalR-хаба PxBlocks.</summary>
    public const string HubRoute = "/_ws/pxblocks";

    /// <summary>Группа рассылки событий исполнения (аналог «nodes» у Mars.Nodes).</summary>
    public const string NotifyGroupName = "pxblocks";
}

/// <summary>Имена методов хаба PxBlocks (сервер → клиент).</summary>
public static class PxBlocksHubMethods
{
    /// <summary>Пакет событий исполнения: (Guid runId, PxExecutionEvent[] events).</summary>
    public const string RunEvents = nameof(RunEvents);

    /// <summary>Запуск завершён: (Guid runId, PxRunResultDto result).</summary>
    public const string RunFinished = nameof(RunFinished);
}
