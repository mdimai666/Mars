using Mars.CodeCompletion.Contracts.Dto;
using Mars.CodeCompletion.Host.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mars.CodeCompletion.Tests;

public class IdleEvictionTests
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMilliseconds(150);

    private static CodeCompletionWorkspaceManager CreateManager()
        => new([new TestCodeContextProvider()], IdleTimeout);

    private static CodePositionRequest Request(string code, string marker = "|")
    {
        var offset = code.IndexOf(marker, StringComparison.Ordinal);
        return new CodePositionRequest
        {
            DocumentId = Guid.NewGuid().ToString("N"),
            Code = offset >= 0 ? code.Replace(marker, "") : code,
            Offset = offset >= 0 ? offset : code.Length,
        };
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Condition was not met within the timeout");
            await Task.Delay(25);
        }
    }

    [Fact]
    public async Task Idle_context_is_evicted_and_recreated_on_next_request()
    {
        using var manager = CreateManager();
        var completion = new CompletionQueryService(manager, NullLogger<CompletionQueryService>.Instance);

        var first = await completion.GetCompletionsAsync(TestCodeContextProvider.Id, Request("msg.|"), CancellationToken.None);
        Assert.Contains(first.Items, i => i.Label == "Payload");
        Assert.Equal(1, manager.ActiveContextCount);

        await WaitFor(() => manager.ActiveContextCount == 0);

        // после эвикции контекст пересоздаётся и completion снова работает
        var second = await completion.GetCompletionsAsync(TestCodeContextProvider.Id, Request("msg.|"), CancellationToken.None);
        Assert.Contains(second.Items, i => i.Label == "Payload");
        Assert.Equal(1, manager.ActiveContextCount);
    }

    [Fact]
    public async Task Active_lease_blocks_eviction()
    {
        using var manager = CreateManager();

        var lease = await manager.GetDocumentAsync(TestCodeContextProvider.Id, Guid.NewGuid().ToString("N"), "msg.", CancellationToken.None);
        await Task.Delay(IdleTimeout * 4);
        Assert.Equal(1, manager.ActiveContextCount);

        lease.Dispose();
        await WaitFor(() => manager.ActiveContextCount == 0);
    }

    [Fact]
    public async Task Leaked_documents_do_not_block_eviction_and_late_remove_is_noop()
    {
        using var manager = CreateManager();
        var documentId = Guid.NewGuid().ToString("N");

        // «утёкший» документ: detach не вызывался (браузер закрыли), RemoveDocument не будет
        using (await manager.GetDocumentAsync(TestCodeContextProvider.Id, documentId, "msg.", CancellationToken.None))
        {
        }

        await WaitFor(() => manager.ActiveContextCount == 0);

        // поздний RemoveDocument по уже эвиктированному контексту — тихий no-op
        manager.RemoveDocument(TestCodeContextProvider.Id, documentId);
    }

    [Fact]
    public async Task Mef_host_is_reset_after_all_contexts_evicted()
    {
        using var manager = CreateManager();
        var generation = manager.HostServicesGeneration;

        using (await manager.GetDocumentAsync(TestCodeContextProvider.Id, Guid.NewGuid().ToString("N"), "msg.", CancellationToken.None))
        {
        }

        await WaitFor(() => manager.HostServicesGeneration > generation);
        Assert.Equal(0, manager.ActiveContextCount);

        // после сброса MEF следующий запрос поднимает инфраструктуру заново
        var hover = new HoverQueryService(manager, NullLogger<HoverQueryService>.Instance);
        var result = await hover.GetHoverAsync(TestCodeContextProvider.Id, Request("msg.Pay|load"), CancellationToken.None);
        Assert.NotNull(result);
    }
}
