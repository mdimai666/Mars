using Mars.Host.Shared.Dto.MetaFields;

namespace Test.Mars.Host.Models;

//db has 3 post with tpyeName: "testPost". content 1,2,3, meta 11,22,33

public class testPost
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";

    public int int1 { get; set; }
    public string str1 { get; set; } = "";

    public static testPost post1()
    {
        return new testPost
        {
            Id = Guid.Empty,
            Title = "testPost1",
            Content = "1",
            int1 = 11,
            str1 = "11"
        };
    }

}

public class testPost2
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";

    public int int1 { get; set; }
    public string str1 { get; set; } = "";
    public Dictionary<string, MetaValueDto> d { get; set; } = default!;
}
