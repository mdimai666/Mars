using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Mars.Host.Shared.Dto.Users;
using Mars.Shared.Contracts.Users;
using Microsoft.AspNetCore.Identity;

namespace Mars.Host.Shared.Dto;

//public interface IXUser : IUserBasicInfo
//{
//    [Display(Name = "Пол")]
//    public UserGender Gender { get; set; }

//    public Guid? GeoRegionId { get; set; }
//    public Guid? GeoMunicipalityId { get; set; }
//    public Guid? GeoLocationId { get; set; }
//}

//[Obsolete]
//public record XUser : UserDetail//, IXUser
//{
//    [Display(Name = "Аватар")]
//    public string? AvatarUrl { get; set; }

//    //public EUserStatus Status { get; set; }

//    public Dictionary<string, object> Meta { get; set; }


//    //-------------GEO-----------
//    [Display(Name = "Регион")]
//    public Guid? GeoRegionId { get; set; }
//    [Display(Name = "Муниципалитет")]
//    public Guid? GeoMunicipalityId { get; set; }
//    [Display(Name = "Поселение")]
//    public Guid? GeoLocationId { get; set; }

//    public XUser(UserDetail user, string avaterUrl)
//    {
//        Id = user.Id;
//        FirstName = user.FirstName;
//        LastName = user.LastName;
//        MiddleName = user.MiddleName;
//        Gender = user.Gender;
//        AvatarUrl = avaterUrl;
//        Created = user.Created;
//        Modified = user.Modified;
//        Status = user.Status;
//        Username = user.UserName!;
//        Email = user.Email;
//        About = user.About;

//        GeoRegionId = user.GeoRegionId;
//        GeoMunicipalityId = user.GeoMunicipalityId;
//        GeoLocationId = user.GeoLocationId;

//        if (user.MetaValues is null)
//        {
//            throw new ArgumentNullException();
//        }

//        Meta = new();

//        foreach (var f in user.MetaValues.Where(s => s.ParentId == Guid.Empty))
//        {
//            Meta.TryAdd(f.MetaField.Key, f.Get());
//        }
//    }

//}

//public class XUserFull : XUser
//{
//    [Display(Name = "День рождения")]
//    [DataType(DataType.Date)]
//    public DateTime BirthDate { get; set; }

//    public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();

//    //-------------GEO-----------
//    public GeoRegion? GeoRegion { get; set; }
//    public GeoMunicipality? GeoMunicipality { get; set; }
//    public GeoLocation? GeoLocation { get; set; }

//    public XUserFull(User user) : base(user)
//    {
//        BirthDate = user.BirthDate;

//        if (GeoRegionId is not null)
//        {
//            if (GeoRegion is null) throw new ArgumentNullException("GeoRegion");
//            GeoRegion = user.GeoRegion;
//        }
//        if (GeoMunicipalityId is not null)
//        {
//            if (GeoMunicipality is null) throw new ArgumentNullException("GeoMunicipality");
//            GeoMunicipality = user.GeoMunicipality;
//        }
//        if (GeoLocationId is not null)
//        {
//            if (GeoLocation is null) throw new ArgumentNullException("GeoLocation");
//            GeoLocation = user.GeoLocation;
//        }
//    }
//}
