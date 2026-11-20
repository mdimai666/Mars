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

        // Формат и блэклист проверяются только при фактической смене имени:
        // легаси-пользователи (username = почта из старого сида, «admin») иначе не могли бы
        // сохранить ни одно обновление профиля.
        RuleFor(x => x.UserName)
            .MustAsync(async (query, userName, context, cancellationToken) =>
            {
                var current = await userRepository.GetDetail(query.Id, cancellationToken);
                if (current is not null && string.Equals(current.UserName, userName, StringComparison.Ordinal))
                    return true;

                var format = await new UserNameRuleValidator().ValidateAsync(userName, cancellationToken);
                var blacklist = await new UsernameBlacklistValidator().ValidateAsync(userName, cancellationToken);

                foreach (var error in format.Errors)
                    context.AddFailure(nameof(UpdateUserQuery.UserName), error.ErrorMessage);
                foreach (var error in blacklist.Errors)
                    context.AddFailure(nameof(UpdateUserQuery.UserName), error.ErrorMessage);

                return format.IsValid && blacklist.IsValid;
            });

        RuleFor(x => x.UserName)
            .MustAsync(async (ctx, x, ct) => !await userRepository.UsernameIsAlreadyTakenByAnotherUser(x, ctx.Id, ct))
            .WithMessage("This username is already taken by another user.");

    }
}
