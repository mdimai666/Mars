using Mars.Core.Models;

namespace Mars.Core.Extensions;

public static class TitleItemExtensions
{
    public static IEnumerable<TitleItem<TKey>> ToTitleItems<TKey>(this IEnumerable<KeyValuePair<TKey, string>> list)
    {
        return list.Select(x => new TitleItem<TKey>(x.Key, x.Value));
    }
}
