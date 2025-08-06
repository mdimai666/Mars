using System.ComponentModel.DataAnnotations;
using Mars.Host.Shared.Dto.Users;

namespace Mars.Host.Shared.Dto.Profile;

public record ProfileDto : UserDetail
{
    //[PersonalData]
    //[Display(Name = "Имя")]
    //public string FirstName { get; set; }
    //[PersonalData]
    //[Display(Name = "Фамилия")]
    //public string LastName { get; set; }
    //[PersonalData]
    //[Display(Name = "Отчество")]
    //public string MiddleName { get; set; }

    //[PersonalData]
    //[NotMapped]
    //[Display(Name = "ФИО")]
    //public string FullName => string.Join(' ', ((string?[])[LastName, FirstName, MiddleName]).TrimNulls());

    //[Display(Name = "Пол")]
    //public Gender Gender { get; set; }

    [Display(Name = "Аватар")]
    public string? AvatarUrl { get; set; }

    //public EUserStatus Status { get; set; }

    public string About { get; set; }

    //[Display(Name = "Email")]
    //public string Email { get; set; }
    [Display(Name = "Телефон")]
    public string Phone { get; set; }

    //[Display(Name = "Логин")]
    //public string UserName { get; set; }

    //[Display(Name = "День рождения")]
    //[DataType(DataType.Date)]
    //public DateTime BirthDate { get; set; }

    //public string[]? Roles { get; set; }

    public ProfileDto()
    {

    }

    public ProfileDto(UserDetail user)
    {
        Id = user.Id;
        //this.Created = user.Created;
        //this.Modified = user.Modified;

        FirstName = user.FirstName;
        LastName = user.LastName;
        MiddleName = user.MiddleName;
        Gender = user.Gender;
        //AvatarUrl = user.AvatarUrl;
        //Status = user.Status;
        //About = user.About;

        Email = user.Email;
        UserName = user.UserName;
        Phone = user.PhoneNumber;
        BirthDate = user.BirthDate;

        Roles = user.Roles;
    }
}
