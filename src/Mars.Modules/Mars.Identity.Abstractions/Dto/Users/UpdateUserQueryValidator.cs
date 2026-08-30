using FluentValidation;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Validators;
using Mars.Identity.Abstractions.Dto.Users.Phones;
using Mars.Identity.Abstractions.Repositories;

namespace Mars.Identity.Abstractions.Dto.Users;

public class UpdateUserQueryValidator : AbstractValidator<UpdateUserQuery>
{
    public UpdateUserQueryValidator(IRoleRepository roleRepository,
                                    IUserRepository userRepository,
                                    IMetaValuesValidator metaValuesValidator)
    {
        RuleFor(x => x.MetaValues).ValidateMetaValues(metaValuesValidator, MetaValueOwnerCatalog.User, x => x.Id);

        RuleFor(x => x.Roles)
            .NotEmpty()
            .MustAsync(roleRepository.RolesExsists)
            .WithMessage(v => $"some roles not exist");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => x.Email != null);

        RuleFor(x => x.PhoneNumber)
            .SetValidator(new UserPhoneValidator())
            .When(x => x.PhoneNumber != null);

        RuleFor(x => x.UserName)
            .SetValidator(new UserNameRuleValidator())
            .SetValidator(new UsernameBlacklistValidator());

        RuleFor(x => x.UserName)
            .MustAsync(async (ctx, x, ct) => !await userRepository.UsernameIsAlreadyTakenByAnotherUser(x, ctx.Id, ct))
            .WithMessage("This username is already taken by another user.");

    }
}
