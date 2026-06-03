namespace Mars.Core.Models;

public record TitleItem<TKey>
{
    public TKey Key { get; set; } = default!;
    public string Title { get; set; } = default!;

    public TitleItem()
    {
    }

    public TitleItem(TKey key, string title)
    {
        Key = key;
        Title = title;
    }
}
