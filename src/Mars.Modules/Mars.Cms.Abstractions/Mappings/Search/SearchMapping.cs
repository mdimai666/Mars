using Mars.Cms.Abstractions.Dto.Search;
using Mars.Contracts.Search;

namespace Mars.Cms.Abstractions.Mappings.Search;

public static class SearchMapping
{
    public static SearchFoundElementResponse ToResponse(this SearchFoundElement entity)
    => new()
    {
        Title = entity.Title,
        Key = entity.Key,
        Type = entity.Type,
        Data = entity.Data,
        Description = entity.Description,
        Url = entity.Url,
        Relevant = entity.Relevant,

    };

    public static IReadOnlyCollection<SearchFoundElementResponse> ToResponse(this IReadOnlyCollection<SearchFoundElement> list)
        => list.Select(ToResponse).ToList();
}
