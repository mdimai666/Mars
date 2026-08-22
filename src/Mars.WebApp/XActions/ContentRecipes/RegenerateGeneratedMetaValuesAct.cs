using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.XActions;

namespace Mars.XActions.ContentRecipes;

/// <summary>
/// Перегенерация значений мета-полей с генераторами (порядковые номера, даты) у существующих постов:
/// генератор могли добавить позже создания записей — старые посты нужно перенумеровать/дозаполнить.
/// </summary>
public class RegenerateGeneratedMetaValuesAct(IMetaValuesGeneratorService generatorService) : IAct
{
    public const string CommandId = "mars.content.regenerateGeneratedMetaValues";
    public const string PostTypeArg = "postType";
    public const string ModeArg = "mode";
    public const string StatusesArg = "statuses";

    public const string ModeAll = "all";
    public const string ModeToday = "today";
    public const string ModeFromLast = "fromLast";

    public async Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        var postTypeName = context.Get(PostTypeArg);
        if (string.IsNullOrWhiteSpace(postTypeName))
            return XActResult.ToastError("не указан тип поста");

        var mode = context.Get(ModeArg) switch
        {
            ModeToday => MetaValuesRegenerationMode.Today,
            ModeFromLast => MetaValuesRegenerationMode.FromLast,
            _ => MetaValuesRegenerationMode.All,
        };

        var statuses = context.Get(StatusesArg)?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var result = await generatorService.RegenerateAsync(new RegenerateMetaValuesQuery
        {
            PostTypeName = postTypeName,
            Mode = mode,
            StatusSlugs = statuses is { Count: > 0 } ? statuses : null,
        }, cancellationToken);

        return result.ValuesUpdated == 0
            ? XActResult.ToastInfo($"без изменений (постов просмотрено: {result.PostsProcessed})")
            : XActResult.ToastSuccess($"перегенерировано значений: {result.ValuesUpdated} (постов: {result.PostsProcessed})");
    }
}
