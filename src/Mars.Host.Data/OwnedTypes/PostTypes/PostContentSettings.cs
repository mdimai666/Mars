namespace Mars.Host.Data.OwnedTypes.PostTypes;

//[jsonb]
public class PostContentSettings
{
    //public string PostContentType { get; set; } = PostTypeConstants.DefaultPostContentTypes.PlainText;
    public string PostContentType { get; set; } = "PlainText";
    //public string[] ContentProcessing { get; set; } = default!;

    public string? CodeLang { get; set; }
}
