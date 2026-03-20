using FluentValidation;

namespace Mars.Host.Shared.Dto.MetaFields;

public class MetaFieldsDuplicateQueryValidator : AbstractValidator<IGeneralMetaFieldsSupportDto>
{
    public MetaFieldsDuplicateQueryValidator()
    {
        RuleForEach(x => x.MetaFields)
            .ChildRules(metaField =>
            {
                metaField.RuleFor(x => x.Key)
                    .NotEmpty()
                    .WithMessage("Key не может быть пустым");
            });

        RuleFor(x => x.MetaFields)
            .Custom((metaFields, context) =>
            {
                var duplicates = metaFields
                    .GroupBy(m => new { m.Key, m.ParentId })
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g.Skip(1));

                foreach (var duplicate in duplicates)
                {
                    var index = metaFields.ToArray().IndexOf(duplicate);

                    var message = duplicate.ParentId == Guid.Empty
                                    ? $"MetaField with key '{duplicate.Key}' дублируется"
                                    : $"MetaField with key '{duplicate.Key}' дублируется для '{duplicate.Title}'({duplicate.ParentId})";

                    context.AddFailure($"MetaFields[{index}].Key", message);
                }
            });
    }
}
