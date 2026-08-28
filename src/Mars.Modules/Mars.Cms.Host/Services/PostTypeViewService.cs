using System.Data;
using System.Text;
using Mars.Core.Exceptions;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Services;
using Mars.Cms.Contracts.MetaFields;
using Microsoft.EntityFrameworkCore;

namespace Mars.Cms.Host.Services;

internal class PostTypeViewService : IPostTypeViewService
{
    const string ViewPrefix = "mt_view_";

    private readonly MarsDbContext _marsDbContext;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;

    public PostTypeViewService(MarsDbContext marsDbContext, IMetaModelTypesLocator metaModelTypesLocator)
    {
        _marsDbContext = marsDbContext;
        _metaModelTypesLocator = metaModelTypesLocator;
    }

    public async Task<string> EnsureViewAsync(string typeName, CancellationToken cancellationToken = default)
    {
        var postType = _metaModelTypesLocator.GetPostTypeByName(typeName)
                        ?? throw new NotFoundException($"post type '{typeName}' not found");

        var viewName = GetViewName(typeName);
        var sql = BuildViewSql(postType, viewName);

        await _marsDbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        return viewName;
    }

    public async Task DropViewAsync(string typeName, CancellationToken cancellationToken = default)
    {
        var viewName = GetViewName(typeName);
        await _marsDbContext.Database.ExecuteSqlRawAsync($"DROP VIEW IF EXISTS \"{viewName}\"", cancellationToken);
    }

    public async Task<IReadOnlyList<T>> ListFromViewAsync<T>(string typeName,
                                                            IEnumerable<string>? properties = null,
                                                            int? take = null,
                                                            CancellationToken cancellationToken = default) where T : new()
    {
        var viewName = await EnsureViewAsync(typeName, cancellationToken);
        var postType = _metaModelTypesLocator.GetPostTypeByName(typeName)!;

        var columns = BuildColumns(postType);
        var selectColumns = properties is null
            ? columns.Select(c => $"\"{c.Property}\"").ToList()
            // column pruning: только запрошенные свойства из известных колонок
            : columns.Where(c => properties.Contains(c.Property, StringComparer.OrdinalIgnoreCase))
                     .Select(c => $"\"{c.Property}\"")
                     .ToList();

        var sql = new StringBuilder($"SELECT {string.Join(", ", selectColumns)} FROM \"{viewName}\"");
        if (take is int limit)
        {
            sql.Append($" LIMIT {limit}");
        }

        var connection = _marsDbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();

        var writableProps = typeof(T).GetProperties()
                                     .Where(p => p.CanWrite)
                                     .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var result = new List<T>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var item = new T();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (!writableProps.TryGetValue(reader.GetName(i), out var prop)) continue;

                if (reader.IsDBNull(i))
                {
                    if (!prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) is not null)
                        prop.SetValue(item, null);
                    continue;
                }

                var value = reader.GetValue(i);
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (!targetType.IsInstanceOfType(value))
                {
                    value = Convert.ChangeType(value, targetType);
                }
                prop.SetValue(item, value);
            }
            result.Add(item);
        }

        return result;
    }

    internal static string GetViewName(string typeName)
    {
        var builder = new StringBuilder(typeName.Length);
        foreach (var ch in typeName.ToLowerInvariant())
        {
            builder.Append(char.IsAsciiLetterOrDigit(ch) || ch == '_' ? ch : '_');
        }
        return ViewPrefix + builder.ToString();
    }

    internal static string BuildViewSql(PostTypeDetail postType, string viewName)
    {
        var columns = BuildColumns(postType);
        var selectList = string.Join(",\n    ", columns.Select(c => $"{c.Expression} AS \"{c.Property}\""));

        return $"""
            CREATE OR REPLACE VIEW "{viewName}" AS
            SELECT
                {selectList}
            FROM "posts" AS "p"
            JOIN "post_types" AS "pt" ON "p"."post_type_id" = "pt"."id"
            WHERE "pt"."type_name" = '{postType.TypeName.Replace("'", "''")}';
            """;
    }

    /// <summary>
    /// Колонки представления: базовые поля поста + разворот мета-значений типа.
    /// Property — имя свойства в моделях (совпадает с Mto), Alias — имя колонки в SQL.
    /// </summary>
    internal static IReadOnlyList<ViewColumnInfo> BuildColumns(PostTypeDetail postType)
    {
        var columns = new List<ViewColumnInfo>
        {
            new("Id", "\"p\".\"id\""),
            new("Slug", "\"p\".\"slug\""),
            new("Title", "\"p\".\"title\""),
            new("CreatedAt", "\"p\".\"created_at\""),
            new("ModifiedAt", "\"p\".\"modified_at\""),
            new("StatusId", "\"p\".\"status_id\""),
            new("UserId", "\"p\".\"user_id\""),
        };

        foreach (var field in postType.MetaFields.OrderBy(f => f.Order))
        {
            if (!TryGetValueColumn(field.Type, out var valueColumn)) continue;

            var property = field.Type switch
            {
                MetaFieldType.Relation or MetaFieldType.File or MetaFieldType.Image => $"{field.Key}Id",
                MetaFieldType.Select => $"{field.Key}VariantId",
                _ => field.Key,
            };

            var expression = $"(SELECT \"mv\".\"{valueColumn}\"" +
                             $" FROM \"post_meta_values\" AS \"mv\"" +
                             $" WHERE \"mv\".\"post_id\" = \"p\".\"id\"" +
                             $" AND \"mv\".\"meta_field_id\" = '{field.Id}')";

            columns.Add(new ViewColumnInfo(property, expression));
        }

        return columns;
    }

    static bool TryGetValueColumn(MetaFieldType type, out string column)
    {
        switch (type)
        {
            case MetaFieldType.String: column = "string_short"; return true;
            case MetaFieldType.Text: column = "string_text"; return true;
            case MetaFieldType.Bool: column = "bool"; return true;
            case MetaFieldType.Int: column = "int"; return true;
            case MetaFieldType.Long: column = "long"; return true;
            case MetaFieldType.Float: column = "float"; return true;
            case MetaFieldType.Decimal: column = "decimal"; return true;
            case MetaFieldType.DateTime: column = "date_time"; return true;
            case MetaFieldType.Select: column = "variant_id"; return true;
            case MetaFieldType.Relation:
            case MetaFieldType.File:
            case MetaFieldType.Image: column = "model_id"; return true;

            // SelectMany (массив) и Query (вычислимое) в плоское представление не раскладываются
            default: column = ""; return false;
        }
    }
}

/// <summary>Property — имя свойства/колонки, Expression — SQL-выражение для неё</summary>
internal record ViewColumnInfo(string Property, string Expression);
