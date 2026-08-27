using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Mars.Host.Data.Entities;
using Mars.Shared.Contracts.Posts;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

/// <summary>
/// Строит EF-транслируемые предикаты фильтрации постов по значениям мета-полей
/// (фильтры колонок грида постов).
/// </summary>
internal static class PostGridMetaFilterExpressions
{
    static readonly PropertyInfo _efFunctions = typeof(EF).GetProperty(nameof(EF.Functions))!;
    static readonly MethodInfo _ilike = typeof(NpgsqlDbFunctionsExtensions)
        .GetMethod(nameof(NpgsqlDbFunctionsExtensions.ILike), [typeof(DbFunctions), typeof(string), typeof(string)])!;

    public static Expression<Func<PostEntity, bool>> Build(Guid metaFieldId, EMetaFieldType fieldType, PostGridFilter filter)
    {
        var post = Expression.Parameter(typeof(PostEntity), "p");

        Expression body = filter.Op switch
        {
            PostGridFilterOps.Empty => Expression.Not(AnyValue(post, metaFieldId, HasValueCondition())),
            PostGridFilterOps.NotEmpty => AnyValue(post, metaFieldId, HasValueCondition()),
            _ => AnyValue(post, metaFieldId, ValueCondition(fieldType, filter)),
        };

        return Expression.Lambda<Func<PostEntity, bool>>(body, post);
    }

    /// <summary>p.MetaValues.Any(v => v.MetaFieldId == id && condition(v))</summary>
    static Expression AnyValue(ParameterExpression post, Guid metaFieldId, Expression<Func<PostMetaValueEntity, bool>> condition)
    {
        var values = Expression.Property(post, nameof(PostEntity.MetaValues));
        var v = condition.Parameters[0];

        var fieldCheck = Expression.Equal(
            Expression.Property(v, nameof(PostMetaValueEntity.MetaFieldId)),
            Expression.Constant(metaFieldId));

        var predicate = Expression.Lambda<Func<PostMetaValueEntity, bool>>(
            Expression.AndAlso(fieldCheck, condition.Body), v);

        return Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), [typeof(PostMetaValueEntity)], values, predicate);
    }

    /// <summary>Значение строки значения заполнено хотя бы в одной колонке</summary>
    static Expression<Func<PostMetaValueEntity, bool>> HasValueCondition()
    {
        var v = Expression.Parameter(typeof(PostMetaValueEntity), "v");

        Expression body = Expression.Constant(false);
        foreach (var column in new[]
        {
            nameof(PostMetaValueEntity.StringShort),
            nameof(PostMetaValueEntity.StringText),
            nameof(PostMetaValueEntity.Bool),
            nameof(PostMetaValueEntity.Int),
            nameof(PostMetaValueEntity.Long),
            nameof(PostMetaValueEntity.Float),
            nameof(PostMetaValueEntity.Decimal),
            nameof(PostMetaValueEntity.DateTime),
            nameof(PostMetaValueEntity.VariantId),
            nameof(PostMetaValueEntity.ModelId),
        })
        {
            body = Expression.OrElse(body, Expression.NotEqual(Expression.Property(v, column), Expression.Constant(null, Expression.Property(v, column).Type)));
        }

        return Expression.Lambda<Func<PostMetaValueEntity, bool>>(body, v);
    }

    /// <summary>Условие по значению в зависимости от типа поля и оператора</summary>
    static Expression<Func<PostMetaValueEntity, bool>> ValueCondition(EMetaFieldType fieldType, PostGridFilter filter)
    {
        var v = Expression.Parameter(typeof(PostMetaValueEntity), "v");
        var body = filter.Op switch
        {
            PostGridFilterOps.Contains when !string.IsNullOrWhiteSpace(filter.Value)
                => OrElse(Ilike(v, nameof(PostMetaValueEntity.StringShort), filter.Value),
                          Ilike(v, nameof(PostMetaValueEntity.StringText), filter.Value)),

            PostGridFilterOps.Eq when !string.IsNullOrWhiteSpace(filter.Value)
                => OrElse(StringEquals(v, nameof(PostMetaValueEntity.StringShort), filter.Value),
                          StringEquals(v, nameof(PostMetaValueEntity.StringText), filter.Value)),

            PostGridFilterOps.In when filter.Values is { Length: > 0 }
                => InCondition(v, fieldType, filter.Values),

            PostGridFilterOps.Gte => Compare(v, fieldType, filter.Value, Expression.GreaterThanOrEqual),
            PostGridFilterOps.Lte => Compare(v, fieldType, filter.Value, Expression.LessThanOrEqual),

            _ => Expression.Constant(false),
        };

        return Expression.Lambda<Func<PostMetaValueEntity, bool>>(body, v);
    }

    static Expression OrElse(Expression left, Expression right) => Expression.OrElse(left, right);

    /// <summary>EF.Functions.ILike(v.column, "%value%") с проверкой на null</summary>
    static Expression Ilike(ParameterExpression v, string column, string value)
    {
        var member = Expression.Property(v, column);
        var pattern = Expression.Constant($"%{value.Trim()}%");

        var call = Expression.Call(_ilike, Expression.Property(null, _efFunctions), member, pattern);
        return Expression.AndAlso(Expression.NotEqual(member, Expression.Constant(null, typeof(string))), call);
    }

    static Expression StringEquals(ParameterExpression v, string column, string value)
    {
        var member = Expression.Property(v, column);
        return Expression.Equal(member, Expression.Constant(value.Trim()));
    }

    static Expression InCondition(ParameterExpression v, EMetaFieldType fieldType, string[] values)
    {
        // Select — по варианту; связь/файл/изображение — по модели; остальное — по строке
        if (fieldType == EMetaFieldType.Select)
            return Contains(v, nameof(PostMetaValueEntity.VariantId), ParseGuids(values));

        if (MetaValueBase.ERelations.Contains(fieldType))
            return Contains(v, nameof(PostMetaValueEntity.ModelId), ParseGuids(values));

        return Contains(v, nameof(PostMetaValueEntity.StringShort), values.ToList());
    }

    /// <summary>values.Contains(v.column)</summary>
    static Expression Contains(ParameterExpression v, string column, object valuesList)
    {
        var member = Expression.Property(v, column);
        var elementType = member.Type switch
        {
            { IsGenericType: true } t => Nullable.GetUnderlyingType(t) ?? t,
            var t => t,
        };

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = Expression.Constant(valuesList, listType);
        var call = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [elementType], list,
            member.Type != elementType ? Expression.Convert(member, elementType) : member);

        return call;
    }

    static object ParseGuids(string[] values)
        => values.Select(s => Guid.TryParse(s, out var id) ? id : (Guid?)null)
                 .Where(id => id is not null)
                 .Select(id => id!.Value)
                 .ToList();

    /// <summary>Сравнение числовой/датной колонки с разобранным значением</summary>
    static Expression Compare(ParameterExpression v,
                              EMetaFieldType fieldType,
                              string? raw,
                              Func<Expression, Expression, Expression> compare)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Expression.Constant(false);

        return fieldType switch
        {
            EMetaFieldType.Int when int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                => CompareColumn(v, nameof(PostMetaValueEntity.Int), Expression.Constant(i), compare),
            EMetaFieldType.Long when long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)
                => CompareColumn(v, nameof(PostMetaValueEntity.Long), Expression.Constant(l), compare),
            EMetaFieldType.Float when double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)
                => CompareColumn(v, nameof(PostMetaValueEntity.Float), Expression.Constant(f), compare),
            EMetaFieldType.Decimal when decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
                => CompareColumn(v, nameof(PostMetaValueEntity.Decimal), Expression.Constant(d), compare),
            EMetaFieldType.DateTime when DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)
                => CompareColumn(v, nameof(PostMetaValueEntity.DateTime), Expression.Constant(dt), compare),
            _ => Expression.Constant(false),
        };
    }

    /// <summary>compare(v.column, constant) с проверкой на null</summary>
    static Expression CompareColumn(ParameterExpression v, string column, ConstantExpression constant, Func<Expression, Expression, Expression> compare)
    {
        var member = Expression.Property(v, column);
        var elementType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;

        return Expression.AndAlso(
            Expression.NotEqual(member, Expression.Constant(null, member.Type)),
            compare(Expression.Convert(member, elementType), Expression.Convert(constant, elementType)));
    }
}
