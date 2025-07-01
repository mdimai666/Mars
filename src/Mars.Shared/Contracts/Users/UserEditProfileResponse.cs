using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mars.Core.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Resources;

namespace Mars.Shared.Contracts.Users;

public class UserEditProfileResponse
{
    [Display(Name = "Id")]
    public required Guid Id { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Имя")]
    public required string FirstName { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Фамилия")]
    public required string LastName { get; set; }

    [Display(Name = "Отчество")]
    public required string? MiddleName { get; set; }

    [Display(Name = "Обо мне")]
    public required string? About { get; set; }

    //[Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [EmailAddress(ErrorMessageResourceName = nameof(AppRes.v_email), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Почта")]
    public required string? Email { get; set; }

    [JsonIgnore]
    [Display(Name = "ФИО")]
    public string FullName => string.Join(' ', ((string?[])[LastName, FirstName, MiddleName]).TrimNulls());

    [Display(Name = "Телефон")]
    public required string? Phone { get; set; }

    [Display(Name = "Пол")]
    public required UserGender Gender { get; set; }

    [Display(Name = "День рождения")]
    [DataType(DataType.Date)]
    public required DateTime? BirthDate { get; set; }

    [Display(Name = "Аватар")]
    public required string? AvatarUrl { get; set; }

    //[Display(Name = "Дополнительные поля")]
    //public virtual ICollection<MetaValue> MetaValues { get; set; }
    //public virtual ICollection<MetaField> MetaFields { get; set; }

    //-------------GEO-----------

    //[Display(Name = "Регион")]
    //public Guid? GeoRegionId { get; set; }

    //[Display(Name = "Муниципалитет")]
    //public Guid? GeoMunicipalityId { get; set; }

    //[Display(Name = "Поселение")]
    //public Guid? GeoLocationId { get; set; }
    //-------------end GEO-----------

    public required string Type { get; init; }
    public required IReadOnlyCollection<MetaValueDetailResponse> MetaValues { get; init; }
}
