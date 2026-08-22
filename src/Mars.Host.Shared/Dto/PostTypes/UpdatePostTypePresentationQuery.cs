using FluentValidation;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Dto.PostTypes;

public record UpdatePostTypePresentationQuery
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Относительный путь к шаблону списка во фронте админки (data/admin/front).
    /// </summary>
    public required string ListViewTemplate { get; init; }

    /// <summary>Настройки грида постов в админке; null — стандартный набор колонок</summary>
    public PostTypeGridSettings? Grid { get; init; }

}

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
