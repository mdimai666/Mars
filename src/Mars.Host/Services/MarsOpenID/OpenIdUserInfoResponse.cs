using System.ComponentModel.DataAnnotations;
using Mars.Host.Shared.Dto.Users;
using Mars.Shared.Contracts.Users;
using Mars.Shared.Resources;

namespace Mars.Host.Services.MarsOpenID;

public class OpenIdUserInfoResponse
{
    [Display(Name = "Id")]
    public Guid Id { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Имя")]
    public string FirstName { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Фамилия")]
    public string LastName { get; set; }

    [Display(Name = "Отчество")]
    public string MiddleName { get; set; }

    [Display(Name = "Обо мне")]
    public string About { get; set; }

    //[Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [EmailAddress(ErrorMessageResourceName = nameof(AppRes.v_email), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Почта")]
    public string Email { get; set; }

    [Display(Name = "Телефон")]
    public string? Phone { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    [Display(Name = "Пол")]
    public UserGender Gender { get; set; }

    [Display(Name = "День рождения")]
    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    [Display(Name = "Аватар")]
    public string? AvatarUrl { get; set; }

    //-------------GEO-----------
    [Display(Name = "Регион")]
    public Guid? GeoRegionId { get; set; }

    [Display(Name = "Муниципалитет")]
    public Guid? GeoMunicipalityId { get; set; }

    [Display(Name = "Поселение")]
    public Guid? GeoLocationId { get; set; }

    public OpenIdUserInfoResponse()
    {
        
    }

    public OpenIdUserInfoResponse(UserDetail user)
    {
        Id = user.Id;
        FirstName = user.FirstName;
        LastName = user.LastName;
        MiddleName = user.MiddleName;
        Email = user.Email;
        //About = user.About;
        Phone = string.IsNullOrEmpty(user.PhoneNumber) ? null : user.PhoneNumber;
        //PhoneNumberConfirmed = user.PhoneNumberConfirmed;
        Gender = user.Gender;
        BirthDate = user.BirthDate == DateTime.MinValue ? null : user.BirthDate;
        //AvatarUrl = string.IsNullOrEmpty(user.AvatarUrl) ? null : user.AvatarUrl;

        //GeoRegionId = user.GeoRegionId;
        //GeoMunicipalityId = user.GeoMunicipalityId;
        //GeoLocationId = user.GeoLocationId;
    }
}
