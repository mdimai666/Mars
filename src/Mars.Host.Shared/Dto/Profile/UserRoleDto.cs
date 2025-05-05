using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mars.Host.Shared.Dto.Profile;

//public class UserRoleDto : User
//{
//    [Display(Name = "Роли")]
//    public IEnumerable<Role> Roles { get; set; }

//    public UserRoleDto()
//    {

//    }

//    public UserRoleDto(User user, IEnumerable<Role> userRoles)
//    {
//        Id = user.Id;
//        FirstName = user.FirstName;
//        LastName = user.LastName;
//        MiddleName = user.MiddleName;
//        BirthDate = user.BirthDate;
//        Email = user.Email;
//        Created = user.Created;
//        Modified = user.Modified;
//        Status = user.Status;
//        UserName = user.UserName;

//        About = user.About;
//        Gender = user.Gender;
//        AvatarUrl = user.AvatarUrl;

//        LockoutEnabled = user.LockoutEnabled;
//        LockoutEnd = user.LockoutEnd;

//        Roles = userRoles ?? new Role[] { };
//        MetaValues = user.MetaValues;
//        MetaFields = user.MetaFields;

//        GeoRegionId = user.GeoRegionId;
//        GeoRegion = user.GeoRegion;
//        GeoMunicipalityId = user.GeoMunicipalityId;
//        GeoMunicipality = user.GeoMunicipality;
//        GeoLocationId = user.GeoLocationId;
//        GeoLocation = user.GeoLocation;
//    }
//}
