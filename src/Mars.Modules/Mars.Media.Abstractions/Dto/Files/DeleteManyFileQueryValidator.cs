using FluentValidation;
using Mars.Media.Abstractions.Repositories;

namespace Mars.Media.Abstractions.Dto.Files;

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
