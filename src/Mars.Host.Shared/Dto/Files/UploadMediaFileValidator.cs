using FluentValidation;
using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Microsoft.AspNetCore.Http;

namespace Mars.Host.Shared.Dto.Files;

public class UploadMediaFileValidator : AbstractValidator<IFormFile>
{
    public UploadMediaFileValidator(IOptionService optionService)
    {
        var mediaOption = optionService.GetOption<MediaOption>();

        RuleFor(x => (ulong)x.Length)
            .LessThanOrEqualTo(mediaOption.MaximumInputFileSize)
            .WithMessage($"max file size is {mediaOption.MaximumInputFileSize.ToHumanizedSize()}");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must((_, filename) =>
            {
                if (mediaOption.IsAllowAllFileTypes) return true;
                var ext = Path.GetExtension(filename).ToLower();
                return mediaOption.AllowedFileExtensions.Contains(ext);
            })
            .WithMessage(x => $"file extension is not allowed '{Path.GetExtension(x.FileName)}'");
    }
}
