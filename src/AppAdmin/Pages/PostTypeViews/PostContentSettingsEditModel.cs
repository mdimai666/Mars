using Aqua.EnumerableExtensions;
using Mars.Shared.Contracts.PostTypes;

namespace AppAdmin.Pages.PostTypeViews;

public record PostContentSettingsEditModel
{
    /// <summary>
    /// <see cref="PostTypeConstants.DefaultPostContentTypes.PlainText"/>
    /// </summary>
    public string PostContentType { get; set; } = PostTypeConstants.DefaultPostContentTypes.BlockEditor;
    public string CodeLang { get; set; } = "";

    public CreatePostContentSettingsRequest ToCreateRequest()
        => new()
        {
            PostContentType = PostContentType,
            CodeLang = CodeLang.AsNullIfEmpty(),
        };

    public UpdatePostContentSettingsRequest ToUpdateRequest()
        => new()
        {
            PostContentType = PostContentType,
            CodeLang = CodeLang.AsNullIfEmpty(),
        };
    
    public static PostContentSettingsEditModel ToModel(PostContentSettingsResponse response)
        => new()
        {
            PostContentType = response.PostContentType,
            CodeLang = response.CodeLang ?? "",
        };
}
