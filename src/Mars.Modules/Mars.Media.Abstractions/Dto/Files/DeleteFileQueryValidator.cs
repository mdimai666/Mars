using FluentValidation;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Repositories;

namespace Mars.Host.Shared.Dto.Files;

public class DeleteFileQueryValidator : AbstractValidator<Guid>
{
    public DeleteFileQueryValidator(IFileRepository fileRepository)
    {
        RuleFor(x => x)
           .CustomAsync(async (id, context, ct) =>
           {
               var fileExist = await fileRepository.ExistAsync(id, ct);

               if (!fileExist)
               {
                   throw new NotFoundException($"post '{id}' not exist");
               }
           });
    }
}
