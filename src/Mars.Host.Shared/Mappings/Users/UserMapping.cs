using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Host.Shared.Mappings.Roles;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Users;
using Mars.Shared.ViewModels;

namespace Mars.Host.Shared.Mappings.Users;

public static class UserMapping
{
    //public static UserSummaryResponse ToResponse(this UserSummary entity)
    //    => new()
    //    {
    //        Id = entity.Id,
    //        FirstName = entity.FirstName,
    //        LastName = entity.LastName,
    //        MiddleName = entity.MiddleName,
    //    };

    public static UserDetailResponse ToResponse(this UserDetail entity)
        => new()
        {
            Id = entity.Id,
            UserName = entity.UserName,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            MiddleName = entity.MiddleName,
            Email = entity.Email,
            PhoneNumber = entity.PhoneNumber,
            Roles = entity.Roles,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            AvatarUrl = entity.AvatarUrl,

            BirthDate = entity.BirthDate,
            Gender = entity.Gender,
            Type = entity.Type,
            MetaValues = entity.MetaValues.ToResponse(),
        };

    public static UserListItemResponse ToResponse(this UserSummary entity)
        => new()
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            MiddleName = entity.MiddleName,
            AvatarUrl = entity.AvatarUrl,
        };

    public static ListDataResult<UserListItemResponse> ToResponse(this ListDataResult<UserSummary> items)
        => items.ToMap(ToResponse);

    public static PagingResult<UserListItemResponse> ToResponse(this PagingResult<UserSummary> items)
        => items.ToMap(ToResponse);

    public static ListDataResult<UserDetailResponse> ToResponse(this ListDataResult<UserDetail> items)
        => items.ToMap(ToResponse);

    public static PagingResult<UserDetailResponse> ToResponse(this PagingResult<UserDetail> items)
        => items.ToMap(ToResponse);

    public static UserListEditViewModelResponse ToResponse(this UserListEditViewModel model)
        => new()
        {
            Users = model.Users.ToMap(ToResponse),
            Roles = model.Roles.Select(RoleMapping.ToResponse).ToList(),
            DefaultSelectRole = model.DefaultSelectRole
        };

    public static UserProfileInfoDto ToProfile(this UserDetail userDetail, int commentCount)
        => new()
        {
            Id = userDetail.Id,
            FirstName = userDetail.FirstName,
            LastName = userDetail.LastName,
            MiddleName = userDetail.MiddleName,
            Email = userDetail.Email,
            Username = userDetail.UserName,
            About = "",
            AvatarUrl = "",
            BirthDate = userDetail.BirthDate,
            Gender = userDetail.Gender,
            Phone = userDetail.PhoneNumber,

            CommentCount = commentCount,
            Roles = userDetail.Roles,
            Type = userDetail.Type,
            MetaValues = userDetail.MetaValues,
        };

    public static UserEditResponse ToResponse(this UserEditDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            UserName = entity.UserName,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            MiddleName = entity.MiddleName,
            Email = entity.Email,
            Phone = entity.PhoneNumber,

            BirthDate = entity.BirthDate,
            Gender = entity.Gender,
            AvatarUrl = entity.AvatarUrl,

            Roles = entity.Roles,
            Type = entity.Type,
            MetaValues = entity.MetaValues.ToDetailResponse(),
        };

    public static UserPrimaryInfo ToPrimaryInfo(this RequestContextUser entity)
        => new()
        {
            Id = entity.Id,
            Username = entity.UserName,
            Email = entity.Email,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Roles = entity.Roles,
            AvatarUrl = entity.AvatarUrl,
        };
}
