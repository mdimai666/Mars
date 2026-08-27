using FluentValidation;

namespace Mars.Host.Shared.Dto.PostTypes;

public class UpdatePostTypePresentationQueryValidator : AbstractValidator<UpdatePostTypePresentationQuery>
{
    public UpdatePostTypePresentationQueryValidator()
    {
        RuleFor(x => x.ListViewTemplate)
            .Must(path => string.IsNullOrWhiteSpace(path) || !path.Replace('\\', '/').Contains(".."))
            .WithMessage(x => $"'{x.ListViewTemplate}' некорректный относительный путь шаблона");

        When(x => x.Grid is not null, () =>
        {
            RuleForEach(x => x.Grid!.Columns)
                .ChildRules(column => column.RuleFor(c => c.Key).NotEmpty());
        });
    }
}
