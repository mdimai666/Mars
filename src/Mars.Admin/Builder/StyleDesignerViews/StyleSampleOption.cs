namespace Mars.Admin.Builder.StyleDesignerViews;

public class StyleSampleOption
{
    public string Name { get; set; } = default!;

    public static IReadOnlyCollection<StyleSampleOption> Items()
    {
        return ItemNames().Select(name => new StyleSampleOption { Name = name }).ToList();
    }

    public static IReadOnlyCollection<string> ItemNames()
    {
        return ["Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliett", "Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "X-ray", "Yankee", "Zulu"];
    }
}
