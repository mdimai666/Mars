using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Mars.Core.Extensions;
using Mars.Shared.Models.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Mars.Host.Shared.Dto;

public interface IUserBasicInfo : IBasicEntity
{
    [PersonalData]
    [Display(Name = "Имя")]
    public string FirstName { get; set; }

    [PersonalData]
    [Display(Name = "Фамилия")]
    public string LastName { get; set; }

    [PersonalData]
    [Display(Name = "Отчество")]
    public string? MiddleName { get; set; }

    [JsonIgnore]
    [PersonalData]
    [NotMapped]
    [Display(Name = "ФИО")]
    public string FullName => string.Join(' ', ((string?[])[LastName, FirstName, MiddleName]).TrimNulls());

    [Display(Name = "Аватар")]
    public string? AvatarUrl { get; set; }
    public string Username { get; set; }

}
