using Mars.Host.Managers;
using Mars.Host.Shared.Managers;
using FluentAssertions;

namespace Test.Mars.Host.EventManagerTests;

public class EventManagerUnitTest
{
    [Fact]
    public void PatternTest()
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
    public void TestMany_Success(bool result, string pattern, string value)
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
    public void TestMany_Fail(bool result, string pattern, string value)
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
    public void TestSpecials(bool result, string pattern, string value)
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

}
