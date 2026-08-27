using Mars.Core.Extensions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Contracts.Users;

namespace Mars.Identity.Abstractions.Dto.Users;

public class UserEditProfileDto
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string? MiddleName { get; init; }
    public required string Username { get; init; }
    public required string? About { get; init; }
    public required string? Email { get; init; }

    public string FullName => string.Join(' ', ((string?[])[LastName, FirstName, MiddleName]).TrimNulls());

    public required string? Phone { get; init; }

    public required UserGender Gender { get; init; }

    public required DateTime? BirthDate { get; init; }

    public required string? AvatarUrl { get; init; }

    public required string Type { get; init; }
    public required IReadOnlyDictionary<string, MetaValueDetailDto> MetaValues { get; init; }

    //-------------GEO-----------

    //public Guid? GeoRegionId { get; init; }

    //public Guid? GeoMunicipalityId { get; init; }

    //public Guid? GeoLocationId { get; init; }
    //-------------end GEO-----------

}
