using FluentValidation;
using Mars.Host.Shared.Utils;

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

                metaField.RuleFor(x => x.Key)
                    .Matches(MetaFieldKeyNormalizer.FormatPattern)
                    .WithMessage("Key должен соответствовать формату [a-z_][a-z0-9_]*");
            });

        RuleFor(x => x.MetaFields)
            .Custom((metaFields, context) =>
            {
                var duplicates = metaFields
                    .GroupBy(m => m.Key)
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g.Skip(1));

                foreach (var duplicate in duplicates)
                {
                    var index = metaFields.ToArray().IndexOf(duplicate);

                    context.AddFailure($"MetaFields[{index}].Key", $"MetaField with key '{duplicate.Key}' дублируется");
                }
            });
    }
}
