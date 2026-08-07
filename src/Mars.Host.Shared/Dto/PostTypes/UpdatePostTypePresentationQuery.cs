using FluentValidation;

namespace Mars.Host.Shared.Dto.PostTypes;

public record UpdatePostTypePresentationQuery
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Относительный путь к шаблону списка во фронте админки (data/admin/front).
    /// </summary>
    public required string ListViewTemplate { get; init; }

}

public class UpdatePostTypePresentationQueryValidator : AbstractValidator<UpdatePostTypePresentationQuery>
{
    public UpdatePostTypePresentationQueryValidator()
    {
        RuleFor(x => x.ListViewTemplate)
            .Must(path => string.IsNullOrWhiteSpace(path) || !path.Replace('\\', '/').Contains(".."))
            .WithMessage(x => $"'{x.ListViewTemplate}' некорректный относительный путь шаблона");
    }
}
