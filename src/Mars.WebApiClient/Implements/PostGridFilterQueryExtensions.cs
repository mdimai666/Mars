using Flurl.Http;
using Mars.Shared.Contracts.Posts;

namespace Mars.WebApiClient.Implements;

/// <summary>
/// Фильтры грида постов для query-строки: массив сложных объектов сериализуем
/// явно индексированным форматом (Filters[0].Key=...), который привязывается
/// модельным связыванием ASP.NET.
/// </summary>
internal static class PostGridFilterQueryExtensions
{
    public static IFlurlRequest AppendGridFilters(this IFlurlRequest request, IReadOnlyCollection<PostGridFilter>? filters)
    {
        if (filters is not { Count: > 0 }) return request;

        var i = 0;
        foreach (var filter in filters)
        {
            request.AppendQueryParam($"Filters[{i}].Key", filter.Key);
            request.AppendQueryParam($"Filters[{i}].Op", filter.Op);

            if (filter.Value is not null)
                request.AppendQueryParam($"Filters[{i}].Value", filter.Value);

            if (filter.Values is not null)
            {
                for (var j = 0; j < filter.Values.Length; j++)
                    request.AppendQueryParam($"Filters[{i}].Values[{j}]", filter.Values[j]);
            }

            i++;
        }

        return request;
    }
}
