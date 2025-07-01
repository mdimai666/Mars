using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Mars.Host.Data.Common;
using Mars.Host.Data.Configurations;
using Mars.Host.Data.OwnedTypes.Users;
using Mars.Core.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[DebuggerDisplay("{UserName}({Email})/{Id}")]
[EntityTypeConfiguration(typeof(UserEntityConfiguration))]
public class UserEntity : IdentityUser<Guid>, IBasicEntity
{

    [PersonalData]
    [Comment("Имя")]
    public string FirstName { get; set; } = default!;

    [PersonalData]
    [Comment("Фамилия")]

    public string LastName { get; set; } = default!;

    [PersonalData]
    [Comment("Отчество")]
    public string? MiddleName { get; set; }

    [PersonalData]
    [NotMapped]
    [Comment("ФИО")]
    public string FullName => string.Join(' ', ((string?[])[LastName, FirstName, MiddleName]).TrimNulls());

    [Comment("День рождения")]
    public DateTime? BirthDate { get; set; }

    [Comment("Пол")]
    public UserGender Gender { get; set; }

    //[Comment("Аватар")]
    //public string? AvatarUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }

    public EUserStatus Status { get; set; }

    //-------------GEO-----------

    //[Comment("Регион")]
    //[ForeignKey(nameof(GeoRegion))]
    //public Guid? GeoRegionId { get; set; }
    //public GeoRegion GeoRegion { get; set; }

    //[Comment("Муниципалитет")]
    //[ForeignKey(nameof(GeoMunicipality))]
    //public Guid? GeoMunicipalityId { get; set; }
    //public GeoMunicipality GeoMunicipality { get; set; }

    //[Comment("Поселение")]
    //[ForeignKey(nameof(GeoLocation))]
    //public Guid? GeoLocationId { get; set; }
    //public GeoLocation GeoLocation { get; set; }
    //-------------end GEO-----------

    //-------------Relations-----------

    public virtual ICollection<PostEntity>? Posts { get; set; }
    public virtual ICollection<FileEntity>? Files { get; set; }

    [ForeignKey(nameof(UserType))]
    public Guid UserTypeId { get; set; }
    public virtual UserTypeEntity? UserType { get; set; }

    [NotMapped]
    public virtual List<MetaValueEntity>? MetaValues { get; set; } = default!;
    public virtual ICollection<UserMetaValueEntity>? UserMetaValues { get; set; } = default!;

    [NotMapped]
    public virtual List<RoleEntity>? Roles { get; set; }
    public virtual ICollection<UserRoleEntity>? UserRoles { get; set; }

    public virtual ICollection<UserClaimEntity>? Claims { get; set; }
    public virtual ICollection<UserLoginEntity>? Logins { get; set; }
    public virtual ICollection<UserTokenEntity>? Tokens { get; set; }

    // Helpers
    public static string StringDigitOnly(string st)
    {
        var rx = new Regex(@"[^0-9]+");
        return rx.Replace(st, "");
    }

    public static string NormalizePhone(string phone)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(phone, nameof(phone));
        phone = Regex.Replace(phone ?? "", "[^0-9+]", "");
        if (phone.Length == 11 && phone.StartsWith("8")) phone = "+7" + phone.Right(10);
        return phone;
    }

    //private static readonly Regex EmailRegex = new Regex("\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*", RegexOptions.IgnoreCase);

    //public static bool IsEmail(string input)
    //    => EmailRegex.Match(input).Success;
}
