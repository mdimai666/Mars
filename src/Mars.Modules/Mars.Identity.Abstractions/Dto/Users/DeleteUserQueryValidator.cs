using FluentValidation;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Repositories;

namespace Mars.Host.Shared.Dto.Users;

public class DeleteUserQueryValidator : AbstractValidator<Guid>
{
    public DeleteUserQueryValidator(IUserRepository userRepository)
    {
        RuleFor(x => x)
            .CustomAsync(async (id, context, ct) =>
            {
                var user = await userRepository.Get(id, ct);

                if (user == null)
                {
                    throw new NotFoundException($"user id='{id}' not exist");
                }

                var admins = await userRepository.ListAll(new() { InRoles = ["Admin"] }, ct);

                if (admins.Count == 1)
                {
                    context.AddFailure(nameof(UserDetail.Roles), $"cannot delete single admin");
                    return;
                }
            });
    }
}
