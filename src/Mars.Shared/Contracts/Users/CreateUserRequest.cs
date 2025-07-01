using System.ComponentModel.DataAnnotations;
using Mars.Core.Extensions;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Resources;

namespace Mars.Shared.Contracts.Users;

public record CreateUserRequest
{
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Имя")]
    public required string FirstName { get; init; }

    [Display(Name = "Фамилия")]
    public required string? LastName { get; init; }

    [Display(Name = "Отчество")]
    public required string? MiddleName { get; init; }

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [EmailAddress(ErrorMessageResourceName = nameof(AppRes.v_email), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Почта")]
    public required string? Email { get; init; }

    [StringLength(30, MinimumLength = 6, ErrorMessageResourceName = nameof(AppRes.v_range), ErrorMessageResourceType = typeof(AppRes))]
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Пароль")]
    public required string Password { get; init; }
    [Display(Name = "Роли")]
    public required IReadOnlyCollection<string> Roles { get; init; } = [];

    public required string? PhoneNumber { get; init; }
    public required DateTime? BirthDate { get; init; }
    public required UserGender Gender { get; init; }

    public required string Type { get; init; }
    public required IReadOnlyCollection<CreateMetaValueRequest> MetaValues { get; init; }
}

public record UpdateUserRequest
{
    public required Guid Id { get; init; }

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Имя")]
    public required string FirstName { get; init; }

    [Display(Name = "Фамилия")]
    public required string? LastName { get; init; }

    [Display(Name = "Отчество")]
    public required string? MiddleName { get; init; }

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [EmailAddress(ErrorMessageResourceName = nameof(AppRes.v_email), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "Почта")]
    public required string? Email { get; init; }

    [Display(Name = "Роли")]
    public required IReadOnlyCollection<string> Roles { get; init; } = [];

    public required string? PhoneNumber { get; init; }
    public required DateTime? BirthDate { get; init; }
    public required UserGender Gender { get; init; }

    [StringLength(1000, MinimumLength = 3)]
    [Required]
    public required string Type { get; init; }
    public required IReadOnlyCollection<UpdateMetaValueRequest> MetaValues { get; init; }
}

public record UserListItemResponse
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string? MiddleName { get; init; }
    public string FullName => string.Join(' ', ((string?[])[LastName, FirstName, MiddleName]).TrimNulls());
}

public record ListUserQueryRequest : BasicListQueryRequest
{
    public IReadOnlyCollection<string>? Roles { get; init; }
}

public record TableUserQueryRequest : BasicTableQueryRequest
{
    public IReadOnlyCollection<string>? Roles { get; init; }
}
