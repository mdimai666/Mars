using System.Text.Json.Nodes;
using Mars.Core.Exceptions;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Handlers;
using Mars.Contracts.MetaFields;

namespace Mars.Cms.Host.Handlers;

/// <summary>
/// Генератор «текущая дата/время»: автозаполнение моментом создания объекта.
/// </summary>
internal class NowValueGeneratorHandler : IMetaValueGeneratorHandler
{
    public Task<object?> GenerateAsync(MetaValueGeneratorContext context, JsonObject? parameters, CancellationToken cancellationToken)
    {
        var field = context.Field;
        if (field.Type != MetaFieldType.DateTime)
            throw MarsValidationException.FromSingleError("generator",
                $"generator 'now' requires field type DateTime, not '{field.Type}' (field '{field.Key}')");

        return Task.FromResult<object?>(context.Now.DateTime);
    }
}
