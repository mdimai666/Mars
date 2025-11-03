using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Users.Passwords;
using Mars.Host.Shared.Extensions;
using Mars.Shared.Contracts.Users;

namespace Mars.Host.Shared.Dto.Users;

public static class UserRequestExtensions
{
    public static CreateUserQuery ToQuery(this CreateUserRequest request, IDictionary<Guid, MetaFieldDto> metaFields)
        => new()
        {
            UserName = request.UserName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = request.Password,
            Roles = request.Roles,

            BirthDate = request.BirthDate,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber,
            Type = request.Type,
            MetaValues = request.MetaValues.ToQuery(metaFields)
        };

    public static UpdateUserQuery ToQuery(this UpdateUserRequest request, IDictionary<Guid, MetaFieldDto> metaFields)
        => new()
        {
            Id = request.Id,
            UserName = request.UserName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            MiddleName = request.MiddleName,
            Email = request.Email,
            Roles = request.Roles,

            BirthDate = request.BirthDate,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber,
            Type = request.Type,
            MetaValues = request.MetaValues.ToQuery(metaFields)
        };

    public static ListUserQuery ToQuery(this ListUserQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,

            Roles = request.Roles
        };

    public static ListUserQuery ToQuery(this TableUserQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,

            Roles = request.Roles
        };

    public static SetUserPasswordByIdQuery ToQuery(this SetUserPasswordByIdRequest request)
        => new()
        {
            UserId = request.UserId,
            NewPassword = request.NewPassword,
        };
}
