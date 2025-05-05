using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mars.Host.Shared.Dto.Profile;

//public class UserEditProfileDto
//{
//    [Display(Name = "Id")]
//    public Guid Id { get; set; }

//    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
//    [Display(Name = "Имя")]
//    public string FirstName { get; set; }

//    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
//    [Display(Name = "Фамилия")]
//    public string LastName { get; set; }

//    [Display(Name = "Отчество")]
//    public string MiddleName { get; set; }

//    [Display(Name = "Обо мне")]
//    public string About { get; set; }

//    //[Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
//    [EmailAddress(ErrorMessageResourceName = nameof(AppRes.v_email), ErrorMessageResourceType = typeof(AppRes))]
//    [Display(Name = "Почта")]
//    public string Email { get; set; }

//    [JsonIgnore]
//    [Newtonsoft.Json.JsonIgnore]
//    [Display(Name = "ФИО")]
//    public string FullName => string.Join(' ', ((string?[])[LastName, FirstName, MiddleName]).TrimNulls()); 


//    [Display(Name = "Телефон")]
//    public string Phone { get; set; }

//    [Display(Name = "Пол")]
//    public Gender Gender { get; set; }

//    [Display(Name = "День рождения")]
//    [DataType(DataType.Date)]
//    public DateTime BirthDate { get; set; }

//    [Display(Name = "Аватар")]
//    public string AvatarUrl { get; set; }

//    [Display(Name = "Дополнительные поля")]
//    public virtual ICollection<MetaValue> MetaValues { get; set; }
//    public virtual ICollection<MetaField> MetaFields { get; set; }

//    //-------------GEO-----------

//    [Display(Name = "Регион")]
//    public Guid? GeoRegionId { get; set; }

//    [Display(Name = "Муниципалитет")]
//    public Guid? GeoMunicipalityId { get; set; }

//    [Display(Name = "Поселение")]
//    public Guid? GeoLocationId { get; set; }
//    //-------------end GEO-----------

//    public UserEditProfileDto()
//    {

//    }

//    public UserEditProfileDto(User user)
//    {
//        Id = user.Id;
//        FirstName = user.FirstName;
//        LastName = user.LastName;
//        MiddleName = user.MiddleName;
//        Email = user.Email;
//        About = user.About;
//        Phone = user.PhoneNumber;
//        Gender = user.Gender;
//        BirthDate = user.BirthDate;
//        AvatarUrl = user.AvatarUrl;

//        MetaValues = user.MetaValues;
//        MetaFields = user.MetaFields;

//        GeoRegionId = user.GeoRegionId;
//        GeoMunicipalityId = user.GeoMunicipalityId;
//        GeoLocationId = user.GeoLocationId;
//    }

//}
