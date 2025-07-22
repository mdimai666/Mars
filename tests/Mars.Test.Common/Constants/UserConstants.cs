using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Users;
using Mars.Shared.Contracts.Users;

namespace Mars.Test.Common.Constants;

public static class UserConstants
{
    public static readonly Guid TestUserId = new("10000000-0000-0000-0001-100000000000");

    public const string TestUserUsername = "testuser";
    public const string TestUserEmail = "testuser@mail.localhost";

    public const string TestUserFirstName = "TestUser";
    public const string TestUserLastName = "TestLastName";

    public const string TestUserPassword = "Password123@";

    public static readonly DateTime TestUserBirthDate = new(1991, 6, 10);

    public static readonly UserDetailTestModel TestUser = new()
    {
        Id = TestUserId,
        FirstName = TestUserFirstName,
        LastName = TestUserLastName,
        UserName = TestUserUsername,
        Email = TestUserEmail,
        SecurityStamp = Guid.NewGuid().ToString(),
        BirthDate = TestUserBirthDate,
        Gender = UserGender.Male,
        MiddleName = null,
        PhoneNumber = "+79990000666",
        Roles = ["Admin"],
        CreatedAt = DateTime.Now,
        ModifiedAt = null,
        Type = UserTypeEntity.DefaultTypeName,
        MetaValues = []
    };

    public static readonly UserTypeEntity TestUserType = new()
    {
        Id = Guid.Empty,
        TypeName = UserTypeEntity.DefaultTypeName,
        Title = UserTypeEntity.DefaultTypeName,
        CreatedAt = DateTimeOffset.Now,
    };

    public static readonly UserEntity TestUserEnt = new()
    {
        Id = TestUserId,
        FirstName = TestUserFirstName,
        LastName = TestUserLastName,
        UserName = TestUserUsername,
        Email = TestUserEmail,
        SecurityStamp = Guid.NewGuid().ToString(),
        Roles = [],
        MetaValues = [],
        UserTypeId = Guid.Empty,
        UserType = TestUserType,
    };

}

public record UserDetailTestModel : UserDetail
{
    public required string SecurityStamp { get; init; }
}
