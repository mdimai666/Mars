using FluentAssertions;
using Mars.Identity.Abstractions.Dto.ApiKeys;
using Mars.Identity.Abstractions.Repositories;
using Mars.Identity.Abstractions.Utils;
using Mars.Identity.Host.Services;
using Mars.Options.Abstractions.Services;
using Mars.Server.Contracts.Options;
using NSubstitute;

namespace Mars.Integration.Tests.Services;

public class ApiKeyServiceTests
{
    private readonly IUserApiKeyRepository _apiKeyRepository = Substitute.For<IUserApiKeyRepository>();
    private readonly IOptionService _optionService = Substitute.For<IOptionService>();
    private readonly ApiKeyService _service;

    public ApiKeyServiceTests()
    {
        _optionService.GetOption<ApiOption>().Returns(new ApiOption { ApiKeysMaxPerUser = 2 });
        _service = new ApiKeyService(_apiKeyRepository, _optionService);
    }

    [Fact]
    public async Task Create_ReturnsFullKeyOnce_AndStoresHashNotSecret()
    {
        Guid storedId = default;
        string? storedHash = null;
        string? storedPrefix = null;
        _apiKeyRepository.Create(Arg.Any<CreateApiKeyQuery>(), Arg.Do<Guid>(g => storedId = g),
                Arg.Do<string>(h => storedHash = h), Arg.Do<string>(p => storedPrefix = p), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var query = new CreateApiKeyQuery { UserId = Guid.NewGuid(), Name = "ci-key" };
        var result = await _service.Create(query, CancellationToken.None);

        result.Ok.Should().BeTrue();
        ApiKeyFormat.TryParse(result.Data.Key, out var keyId, out var secret).Should().BeTrue();
        keyId.Should().Be(result.Data.Id);
        storedId.Should().Be(result.Data.Id);
        storedHash.Should().Be(ApiKeyFormat.HashSecret(secret));
        storedHash.Should().NotBe(secret);
        storedPrefix.Should().Be(ApiKeyFormat.DisplayPrefix(result.Data.Id));
    }

    [Fact]
    public async Task Create_RejectsWhenLimitReached()
    {
        _apiKeyRepository.CountByUser(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(2);

        var result = await _service.Create(new CreateApiKeyQuery { UserId = Guid.NewGuid(), Name = "ci-key" }, CancellationToken.None);

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("лимит");
        await _apiKeyRepository.DidNotReceive().Create(Arg.Any<CreateApiKeyQuery>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_RejectsPastExpiration()
    {
        var query = new CreateApiKeyQuery
        {
            UserId = Guid.NewGuid(),
            Name = "ci-key",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
        };

        var result = await _service.Create(query, CancellationToken.None);

        result.Ok.Should().BeFalse();
    }

    [Fact]
    public async Task Revoke_ReportsSuccessOnlyWhenKeyDeleted()
    {
        var userId = Guid.NewGuid();
        var keyId = Guid.NewGuid();
        _apiKeyRepository.Delete(keyId, userId, Arg.Any<CancellationToken>()).Returns(true);

        (await _service.Revoke(keyId, userId, CancellationToken.None)).Ok.Should().BeTrue();

        var otherKeyId = Guid.NewGuid();
        _apiKeyRepository.Delete(otherKeyId, userId, Arg.Any<CancellationToken>()).Returns(false);

        (await _service.Revoke(otherKeyId, userId, CancellationToken.None)).Ok.Should().BeFalse();
    }
}
