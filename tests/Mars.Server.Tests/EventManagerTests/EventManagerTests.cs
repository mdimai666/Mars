using FluentAssertions;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Managers;

namespace Mars.Server.Tests.EventManagerTests;

public class EventManagerTests
{
    [Fact]
    public void TestTopic_WildcardMiddleSegment_Matches()
    {
        //var postAdd = eventManager.Defaults.PostAdd("post");
        var postAdd = "entity.post/post/add";
        var pattern = "entity.post/*/add";

        IEventManager.TestTopic(pattern, postAdd).Should().BeTrue();
    }



    [Theory]
    [InlineData(true, "*", "entity.post/post/add")]
    [InlineData(true, "entity.post/post/add", "entity.post/post/add")]
    [InlineData(true, "entity.post/*/add", "entity.post/post/add")]
    [InlineData(true, "entity.post/*/*", "entity.post/post/add")]
    [InlineData(true, "entity.post/**", "entity.post/post/add")]
    [InlineData(true, "option/*", "option/update")]
    [InlineData(true, "option/update", "option/update")]
    public void TestTopic_MatchingPatterns_ReturnsTrue(bool result, string pattern, string value)
    {
        IEventManager.TestTopic(pattern, value).Should().Be(result);
    }

    [Theory]
    [InlineData(false, "*", "")]
    [InlineData(false, "entity.post/*/update", "entity.post/post/add")]
    [InlineData(false, "entity.post/post/update", "entity.post/post/add")]
    [InlineData(false, "entity.post/post/update", "entity.post/post/update/1")]
    [InlineData(false, "entity.post/post/update/1", "entity.post/post/update")]
    [InlineData(false, "option/update", "entity.post/post/add")]
    public void TestTopic_NonMatchingPatterns_ReturnsFalse(bool result, string pattern, string value)
    {
        IEventManager.TestTopic(pattern, value).Should().Be(result);
    }

    [Theory]
    [InlineData(true, "entity.post/[post,page]/update", "entity.post/page/update")]
    [InlineData(true, "entity.post/[post, page]/update", "entity.post/page/update")]
    [InlineData(false, "entity.post/[post,page]/update", "entity.post/page/add")]
    [InlineData(true, "entity.post/post/[add,update]", "entity.post/post/add")]
    [InlineData(false, "entity.post/post/[add,update]", "entity.post/page/add")]
    [InlineData(false, "[add,update]/page/add", "entity.post/page/add")]
    public void TestTopic_BracketChoicePatterns_MatchesByChoice(bool result, string pattern, string value)
    {
        IEventManager.TestTopic(pattern, value).Should().Be(result);
    }

    [Fact]
    public async Task TriggerEvent_Success()
    {
        var eventManager = new EventManager();

        int triggeredCount = 0;

        eventManager.AddEventListener("entity.post/post/add", payload =>
        {
            triggeredCount++;
        });

        eventManager.AddEventListener("entity.post/post/*", payload =>
        {
            triggeredCount++;
        });

        eventManager.AddEventListener("entity.post/post/[update,add]", payload =>
        {
            triggeredCount++;
        });

        eventManager.AddEventListener("entity.post/*/add", payload =>
        {
            triggeredCount++;
        });

        eventManager.AddEventListener("entity.post/post/**", payload =>
        {
            triggeredCount++;
        });

        eventManager.AddEventListener("entity.post/fail/**", payload =>
        {
            triggeredCount++;
        });

        eventManager.AddEventListener("entity.post/post/delete", payload =>
        {
            triggeredCount++;
        });

        eventManager.TriggerEvent(new ManagerEventPayload("entity.post/post/add", new { }));

        await Task.Delay(10);

        triggeredCount.Should().Be(5);
    }

    [Theory]
    [InlineData(false, "entity/**/add", "entity/post/add")]
    [InlineData(false, "**/post/add", "entity/post/add")]
    [InlineData(false, "", "entity/post/add")]
    [InlineData(false, "entity/post/add", "")]
    public void TestTopic_EdgePatterns_ReturnsFalse(bool result, string pattern, string value)
    {
        IEventManager.TestTopic(pattern, value).Should().Be(result);
    }

    [Fact]
    public void RemoveEventListener_SameDelegate_Unsubscribes()
    {
        var eventManager = new EventManager();
        var triggeredCount = 0;
        Action<ManagerEventPayload> handler = _ => triggeredCount++;

        eventManager.AddEventListener("entity.post/post/add", handler);
        eventManager.RemoveEventListener("entity.post/post/add", handler);

        eventManager.TriggerEvent(new ManagerEventPayload("entity.post/post/add", new { }));

        triggeredCount.Should().Be(0);
    }

    [Fact]
    public void TriggerEvent_WildcardFirstSegmentPattern_Fires()
    {
        var eventManager = new EventManager();
        var triggeredCount = 0;

        eventManager.AddEventListener("*/post/add", _ => triggeredCount++);

        eventManager.TriggerEvent(new ManagerEventPayload("entity.post/post/add", new { }));

        triggeredCount.Should().Be(1);
    }

    [Fact]
    public void TriggerEvent_CaseInsensitiveTopic_Fires()
    {
        var eventManager = new EventManager();
        var triggeredCount = 0;

        eventManager.AddEventListener("entity.post/post/add", _ => triggeredCount++);

        eventManager.TriggerEvent(new ManagerEventPayload("Entity.Post/Post/Add", new { }));

        triggeredCount.Should().Be(1);
    }

    [Fact]
    public void DeclaredEvents_ReturnsDistinctSubscribedTopics()
    {
        var eventManager = new EventManager();

        eventManager.AddEventListener("entity.post/post/add", _ => { });
        eventManager.AddEventListener("entity.post/post/add", _ => { });
        eventManager.AddEventListener("entity.post/*/add", _ => { });

        eventManager.DeclaredEvents().Select(s => s.Key).Should()
            .BeEquivalentTo(["entity.post/post/add", "entity.post/*/add"]);
    }

    [Fact]
    public async Task ConcurrentAddAndTrigger_NoExceptions_AllSubscribersFire()
    {
        var eventManager = new EventManager();
        const int threadCount = 8;
        const int perThread = 200;
        var fired = new int[threadCount * perThread];

        using var barrier = new Barrier(threadCount + 1);

        var addTasks = Enumerable.Range(0, threadCount).Select(t => Task.Run(() =>
        {
            barrier.SignalAndWait();
            for (var i = 0; i < perThread; i++)
            {
                var index = t * perThread + i;
                eventManager.AddEventListener($"entity.post/type{index}/add", _ => Interlocked.Increment(ref fired[index]));
            }
        })).ToArray();

        var triggerTask = Task.Run(() =>
        {
            barrier.SignalAndWait();
            for (var i = 0; i < perThread; i++)
            {
                eventManager.TriggerEvent(new ManagerEventPayload("entity.post/type0/add", new { }));
            }
        });

        await Task.WhenAll([.. addTasks, triggerTask]);

        eventManager.TriggerEvent(new ManagerEventPayload("entity.post/type0/add", new { }));

        Volatile.Read(ref fired[0]).Should().BeGreaterThanOrEqualTo(1);
        eventManager.DeclaredEvents().Should().HaveCount(threadCount * perThread);
    }
}
