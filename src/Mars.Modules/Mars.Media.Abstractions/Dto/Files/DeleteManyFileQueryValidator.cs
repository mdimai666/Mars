using FluentValidation;
using Mars.Host.Shared.Repositories;

namespace Mars.Host.Shared.Dto.Files;

public class DeleteManyFileQueryValidator : AbstractValidator<DeleteManyFileQuery>
{
    public DeleteManyFileQueryValidator(IFileRepository fileRepository)
    {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .WithMessage("File ids for delete must not be empty");

        RuleForEach(x => x.Ids)
            .SetValidator(new DeleteFileQueryValidator(fileRepository));
    }
}
