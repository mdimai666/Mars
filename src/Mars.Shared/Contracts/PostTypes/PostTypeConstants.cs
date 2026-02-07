namespace Mars.Shared.Contracts.PostTypes;

public sealed class PostTypeConstants
{
    public const int TypeNameMinLength = 3;
    public const int TypeNameMaxLength = 128;

    public sealed class DefaultPostContentTypes
    {
        public static readonly string[] All = [PlainText, WYSIWYG, Code, BlockEditor];

        public const string PlainText = "PlainText";
        public const string WYSIWYG = "WYSIWYG";
        public const string Code = "Code";
        public const string BlockEditor = "BlockEditor";

        public const string DefaultCodeTemplate = "handlebars";
    }

    public sealed class Features
    {
        public static readonly string[] All = [
            Content, Status, ModifyCreatedDate,
            Language, Tags, Excerpt, Category
        ];

        public const string Content = "Content";
        public const string Status = "Status";
        public const string ModifyCreatedDate = "ModifyCreatedDate";
        public const string Language = "Language";
        public const string Tags = "Tags";
        public const string Excerpt = "Excerpt";
        public const string Category = "Category";
    }

}
