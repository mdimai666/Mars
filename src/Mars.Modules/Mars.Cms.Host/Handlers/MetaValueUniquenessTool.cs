using Mars.Data.Entities;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Contracts.MetaFields;
using Microsoft.EntityFrameworkCore;

namespace Mars.Cms.Host.Handlers;

/// <summary>
/// Общая проверка уникальности по типизированной колонке значения
/// для провайдеров доменов (<see cref="Mars.Cms.Abstractions.Services.IMetaValueUniquenessProvider"/>)
/// </summary>
internal static class MetaValueUniquenessTool
{
    /// <summary>Наличие значения в типизированной колонке по типу поля;
    /// неподдерживаемый тип или неожиданный тип значения — «не занято»</summary>
    public static ValueTask<bool> CheckAsync<TValue>(IQueryable<TValue> values, MetaFieldDto field, object? value,
                                                     CancellationToken cancellationToken)
        where TValue : MetaValueBase
    {
        switch (field.Type)
        {
            case MetaFieldType.String when value is string stringShort:
                return new ValueTask<bool>(values.AnyAsync(v => v.StringShort == stringShort, cancellationToken));

            case MetaFieldType.Text when value is string stringText:
                return new ValueTask<bool>(values.AnyAsync(v => v.StringText == stringText, cancellationToken));

            case MetaFieldType.Int when value is int intValue:
                return new ValueTask<bool>(values.AnyAsync(v => v.Int == intValue, cancellationToken));

            case MetaFieldType.Long when value is long longValue:
                return new ValueTask<bool>(values.AnyAsync(v => v.Long == longValue, cancellationToken));

            case MetaFieldType.Float when value is double floatValue:
                return new ValueTask<bool>(values.AnyAsync(v => v.Float == floatValue, cancellationToken));

            case MetaFieldType.Decimal when value is decimal decimalValue:
                return new ValueTask<bool>(values.AnyAsync(v => v.Decimal == decimalValue, cancellationToken));

            case MetaFieldType.DateTime when value is DateTime dateTime:
                return new ValueTask<bool>(values.AnyAsync(v => v.DateTime == dateTime, cancellationToken));

            default:
                return ValueTask.FromResult(false);
        }
    }
}
