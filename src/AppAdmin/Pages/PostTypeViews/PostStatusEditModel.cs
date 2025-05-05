using Mars.Shared.Contracts.PostTypes;

namespace AppAdmin.Pages.PostTypeViews;

public record PostStatusEditModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public List<string> Tags { get; set; } = [];

    public CreatePostStatusRequest ToCreateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            Slug = Slug,
            Tags = Tags,
        };

    public UpdatePostStatusRequest ToUpdateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            Slug = Slug,
            Tags = Tags,
        };

    public static PostStatusEditModel ToModel(PostStatusResponse response)
        => new()
        {
            Id = response.Id,
            Title = response.Title,
            Slug = response.Slug,
            Tags = response.Tags.ToList(),
        };

    public static List<PostStatusEditModel> DefaultStatuses()
    {
        return new List<PostStatusEditModel>
        {
            new ()
            {
                Id = Guid.NewGuid(),
                Title = "Черновик",
                Slug = "draft",
            },
            new ()
            {
                Id = Guid.NewGuid(),
                Title = "На проверке",
                Slug = "pending",
            },
            new ()
            {
                Id = Guid.NewGuid(),
                Title = "Опубликовано",
                Slug = "publish",
            },
            new ()
            {
                Id = Guid.NewGuid(),
                Title = "Скрыто",
                Slug = "hidden",
            },
            new ()
            {
                Id = Guid.NewGuid(),
                Title = "Удалено",
                Slug = "trash",
            },
        };
    }
}
