using FluentAssertions;
using Mars.Identity.Abstractions.Utils;

namespace Mars.Server.Tests.Utils;

public class ApiKeyFormatTests
{
    [Fact]
    public void BuildThenParse_RoundTrips()
    {
        var keyId = Guid.NewGuid();
        var secret = ApiKeyFormat.GenerateSecret();

        var key = ApiKeyFormat.Build(keyId, secret);

        key.Should().StartWith(ApiKeyFormat.Prefix);
        ApiKeyFormat.TryParse(key, out var parsedId, out var parsedSecret).Should().BeTrue();
        parsedId.Should().Be(keyId);
        parsedSecret.Should().Be(secret);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("mars_")]
    [InlineData("sk_4f2e2b1a-0000-0000-0000-000000000000.secret")]
    [InlineData("mars_not-a-guid.secret")]
    [InlineData("mars_3f2a2b1a000000000000000000000000")]
    public void TryParse_RejectsInvalidKeys(string? key)
    {
        ApiKeyFormat.TryParse(key, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_RejectsEmptySecret()
    {
        var key = $"mars_{Guid.NewGuid()}.";

        ApiKeyFormat.TryParse(key, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void HashSecret_IsStableSha256Base64()
    {
        var hash1 = ApiKeyFormat.HashSecret("secret");
        var hash2 = ApiKeyFormat.HashSecret("secret");

        hash1.Should().Be(hash2);
        Convert.FromBase64String(hash1).Should().HaveCount(32);
    }

    [Fact]
    public void SecretMatchesHash_RejectsWrongSecret()
    {
        var hash = ApiKeyFormat.HashSecret("right-secret");

        ApiKeyFormat.SecretMatchesHash("right-secret", hash).Should().BeTrue();
        ApiKeyFormat.SecretMatchesHash("wrong-secret", hash).Should().BeFalse();
    }

    [Fact]
    public void GenerateSecret_IsUnique()
    {
        ApiKeyFormat.GenerateSecret().Should().NotBe(ApiKeyFormat.GenerateSecret());
    }

    [Fact]
    public void DisplayPrefix_StartsWithProductPrefix()
    {
        ApiKeyFormat.DisplayPrefix(Guid.NewGuid()).Should().StartWith(ApiKeyFormat.Prefix).And.EndWith("…");
    }
}
