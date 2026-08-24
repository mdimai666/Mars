namespace Mars.Shared.Contracts.PostTypes;

public sealed class PostTypeConstants
{
    public const int TypeNameMinLength = 3;
    public const int TypeNameMaxLength = 128;

    public sealed class Features
    {
        public static readonly string[] All = [
            Content, Status, ModifyCreatedDate,
            Language, Tags, Excerpt, Category, PostImage, Single
        ];

        public const string Content = "Content";
        public const string Status = "Status";
        public const string ModifyCreatedDate = "ModifyCreatedDate";
        public const string Language = "Language";
        public const string Tags = "Tags";
        public const string Excerpt = "Excerpt";
        public const string Category = "Category";
        public const string PostImage = "PostImage";
        public const string Single = "Single";
    }

}
