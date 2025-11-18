using System.ComponentModel.DataAnnotations;

namespace Mars.Shared.ViewModels;

public static class RoleCaps
{
    public static class PostCap
    {
        public const string Add = "Post.Add";
        public const string Update = "Post.Update";
        public const string Delete = "Post.Delete";
        //public const string ChangeStatus = "Post.ChangeStatus";

        public static class CommentCap
        {
            public const string Add = "Post.Comment.Add";
            public const string Delete = "Post.Comment.Delete";
            public const string ViewAll = "Post.Comment.ViewAll";
        }
    }

    public static class UserCap
    {
        public const string Add = "User.Add";
        public const string Update = "User.Update";
        public const string Delete = "User.Delete";
        public const string View = "User.View";
        public const string UpdateRole = "User.UpdateRole";
    }

    [Display(Name = "Типы записей")]
    public static class PostTypeCap
    {
        [Display(Name = "Управлять всем")]
        public const string Manage = "PostType.Manage";

    }

    [Display(Name = "Навигационное меню")]
    public static class NavMenuCap
    {
        public const string Manage = "PostType.Manage";
    }

    public static class ContactPersonCap
    {
        public const string Add = "ContactPerson.Add";
        public const string Update = "ContactPerson.Update";
        public const string Delete = "ContactPerson.Delete";
    }

    public static class GeoLocationCap
    {
        public const string Add = "GeoLocation.Add";
        public const string Update = "GeoLocation.Update";
        public const string Delete = "GeoLocation.Delete";
    }

}

public class RoleCapElement
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; } 

    public RoleCapElement()
    {

    }
    public RoleCapElement(string id, string title, string description)
    {
        Id = id;
        Title = title;
        Description = description;
    }

}

public class RoleCapElementCheckable : RoleCapElement
{
    public bool Checked { get; set; }

    public RoleCapElementCheckable()
    {

    }

    public RoleCapElementCheckable(RoleCapElement cap)
    {
        this.Id = cap.Id;
        this.Title = cap.Title;
        this.Description = cap.Description;

    }
}

public class RoleCapGroup
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RoleCapElement[] RoleCapElements { get; set; } = [];
}

public class RoleCapGroupCheckable
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public RoleCapElementCheckable[] RoleCapElements { get; set; } = [];

    public RoleCapGroupCheckable()
    {

    }
    public RoleCapGroupCheckable(RoleCapGroup roleCapGroup)
    {
        this.Title = roleCapGroup.Title;
        this.Description = roleCapGroup.Description;
        this.RoleCapElements = roleCapGroup.RoleCapElements.Select(cap => new RoleCapElementCheckable(cap)).ToArray();
    }
}
