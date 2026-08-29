using Mars.Cms.Contracts.PostTypes;

namespace Mars.Admin.Pages.PostTypeViews;

public record PostStatusEditModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Color { get; set; } = "";
    public int Order { get; set; }

    public CreatePostStatusRequest ToCreateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            Slug = Slug,
            Color = Color,
            Order = Order,
        };

    public UpdatePostStatusRequest ToUpdateRequest()
        => new()
        {
            Id = Id,
            Title = Title,
            Slug = Slug,
            Color = Color,
            Order = Order,
        };

    public static PostStatusEditModel ToModel(PostStatusResponse response)
        => new()
        {
            Id = response.Id,
            Title = response.Title,
            Slug = response.Slug,
            Color = response.Color,
            Order = response.Order,
        };

    public static List<PostStatusEditModel> DefaultStatuses()
    {
        return
        [
            new ()
            {
                Id = Guid.NewGuid(),
                Title = "Черновик",
                Slug = "draft",
                Order = 0,
            },
            new ()
            {
                Id = Guid.NewGuid(),
                Title = "На проверке",
                Slug = "pending",
                Order = 1,
            },
            new ()
            {
                Id = Guid.NewGuid(),
                Title = "Опубликовано",
                Slug = "publish",
                Order = 2,
            },
            new ()
            {
                Id = Guid.NewGuid(),
                Title = "Скрыто",
                Slug = "hidden",
                Order = 3,
            },
            new ()
            {
                Id = Guid.NewGuid(),
                Title = "Удалено",
                Slug = "trash",
                Order = 4,
            },
        ];
    }
}
