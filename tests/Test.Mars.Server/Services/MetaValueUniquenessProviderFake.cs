using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Utils;

namespace Test.Mars.Server.Services;

/// <summary>
/// Фейк <see cref="IMetaValueUniquenessProvider"/> для юнит-тестов:
/// результат задаётся делегатом, вызовы записываются
/// </summary>
public class MetaValueUniquenessProviderFake : IMetaValueUniquenessProvider
{
    public Func<MetaFieldDto, object?, Guid?, bool> IsOccupiedHandler { get; set; } = (_, _, _) => false;

    public List<(Guid FieldId, object? Value, Guid? ExcludeOwnerId)> Calls { get; } = [];

    public ValueTask<bool> IsOccupiedAsync(MetaFieldDto field, object? value, Guid? excludeOwnerId, CancellationToken cancellationToken)
    {
        Calls.Add((field.Id, value, excludeOwnerId));
        return ValueTask.FromResult(IsOccupiedHandler(field, value, excludeOwnerId));
    }
}
