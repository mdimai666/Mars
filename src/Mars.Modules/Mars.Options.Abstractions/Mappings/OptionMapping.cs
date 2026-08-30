using Mars.Options.Abstractions.Dto;
using Mars.Options.Contracts.Dto.Options;

namespace Mars.Options.Abstractions.Mappings;

public static class OptionMapping
{
    public static OptionSummaryResponse ToResponse(this OptionSummary entity)
     => new()
     {
         Key = entity.Key,
         Type = entity.Type,
         Value = entity.Value
     };

    public static OptionDetailResponse ToResponse(this OptionDetail entity)
     => new()
     {
         Id = entity.Id,
         CreatedAt = entity.CreatedAt,
         ModifiedAt = entity.ModifiedAt,
         Key = entity.Key,
         Type = entity.Type,
         Value = entity.Value
     };
}
