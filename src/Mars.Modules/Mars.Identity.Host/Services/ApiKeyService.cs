using Mars.Contracts.Common;
using Mars.Identity.Abstractions.Dto.ApiKeys;
using Mars.Identity.Abstractions.Repositories;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Abstractions.Utils;
using Mars.Options.Abstractions.Services;
using Mars.Server.Contracts.Options;

namespace Mars.Identity.Host.Services;

internal class ApiKeyService : IApiKeyService
{
    private readonly IUserApiKeyRepository _apiKeyRepository;
    private readonly IOptionService _optionService;

    public ApiKeyService(IUserApiKeyRepository apiKeyRepository, IOptionService optionService)
    {
        _apiKeyRepository = apiKeyRepository;
        _optionService = optionService;
    }

    public async Task<UserActionResult<CreatedApiKeyDto>> Create(CreateApiKeyQuery query, CancellationToken cancellationToken)
    {
        if (query.ExpiresAt.HasValue && query.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            return UserActionResult<CreatedApiKeyDto>.Exception("Срок действия ключа должен быть в будущем");

        var limit = _optionService.GetOption<ApiOption>().ApiKeysMaxPerUser;
        var count = await _apiKeyRepository.CountByUser(query.UserId, cancellationToken);

        if (count >= limit)
            return UserActionResult<CreatedApiKeyDto>.Exception($"Достигнут лимит API-ключей на пользователя ({limit})");

        var id = Guid.NewGuid();
        var secret = ApiKeyFormat.GenerateSecret();

        await _apiKeyRepository.Create(query, id, ApiKeyFormat.HashSecret(secret), ApiKeyFormat.DisplayPrefix(id), cancellationToken);

        return UserActionResult<CreatedApiKeyDto>.Success(new CreatedApiKeyDto
        {
            Id = id,
            Name = query.Name,
            KeyPrefix = ApiKeyFormat.DisplayPrefix(id),
            CreatedAt = DateTimeOffset.Now,
            ExpiresAt = query.ExpiresAt,
            Key = ApiKeyFormat.Build(id, secret),
        });
    }

    public Task<IReadOnlyCollection<ApiKeySummary>> ListByUser(Guid userId, CancellationToken cancellationToken)
        => _apiKeyRepository.ListByUser(userId, cancellationToken);

    public async Task<UserActionResult> Revoke(Guid keyId, Guid userId, CancellationToken cancellationToken)
    {
        var deleted = await _apiKeyRepository.Delete(keyId, userId, cancellationToken);

        return deleted
            ? UserActionResult.SuccessDeleted()
            : UserActionResult.Exception("Ключ не найден", null);
    }
}
