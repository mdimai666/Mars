using System.ComponentModel.DataAnnotations;

namespace Mars.Shared.ViewModels;

public static class RoleCaps
{
    public class PostCap
    {
        public const string Add = "Post.Add";
        public const string Update = "Post.Update";
        public const string Delete = "Post.Delete";
        //public const string ChangeStatus = "Post.ChangeStatus";

        public class CommentCap
        {
            public const string Add = "Post.Comment.Add";
            public const string Delete = "Post.Comment.Delete";
            public const string ViewAll = "Post.Comment.ViewAll";
        }
    }

    public class UserCap
    {
        public const string Add = "User.Add";
        public const string Update = "User.Update";
        public const string Delete = "User.Delete";
        public const string View = "User.View";
        public const string UpdateRole = "User.UpdateRole";
    }

    [Display(Name = "Типы записей")]
    public class PostTypeCap
    {
        [Display(Name = "Управлять всем")]
        public const string Manage = "PostType.Manage";

    }

    [Display(Name = "Навигационное меню")]
    public class NavMenuCap
    {
        public const string Manage = "PostType.Manage";
    }

    public class ContactPersonCap
    {
        public const string Add = "ContactPerson.Add";
        public const string Update = "ContactPerson.Update";
        public const string Delete = "ContactPerson.Delete";
    }

    public class GeoLocationCap
    {
        public const string Add = "GeoLocation.Add";
        public const string Update = "GeoLocation.Update";
        public const string Delete = "GeoLocation.Delete";
    }



}

public class RoleCapElement
{
    public string Id { get; set; }
    public string Title { get; set; }
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
    public string Title { get; set; }
    public string? Description { get; set; }
    public RoleCapElement[] RoleCapElements { get; set; }
}



public class RoleCapGroupCheckable
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public RoleCapElementCheckable[] RoleCapElements { get; set; }

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
