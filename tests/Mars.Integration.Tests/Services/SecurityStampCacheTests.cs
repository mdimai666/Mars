using FluentAssertions;
using Mars.Identity.Host.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Integration.Tests.Services;

public class SecurityStampCacheTests
{
    [Fact]
    public void Mark_TryGetNewStamp_Roundtrip()
    {
        var cache = new SecurityStampCache(new MemoryCache(new MemoryCacheOptions()));
        var userId = Guid.NewGuid();

        cache.TryGetNewStamp(userId, out _).Should().BeFalse();

        cache.Mark(userId, "stamp-1");
        cache.TryGetNewStamp(userId, out var stamp).Should().BeTrue();
        stamp.Should().Be("stamp-1");

        cache.Mark(userId, "stamp-2");
        cache.TryGetNewStamp(userId, out stamp).Should().BeTrue();
        stamp.Should().Be("stamp-2");
    }

    [Fact]
    public void TryGetNewStamp_OtherUser_NotAffected()
    {
        var cache = new SecurityStampCache(new MemoryCache(new MemoryCacheOptions()));
        cache.Mark(Guid.NewGuid(), "stamp-1");

        cache.TryGetNewStamp(Guid.NewGuid(), out _).Should().BeFalse();
    }
}
