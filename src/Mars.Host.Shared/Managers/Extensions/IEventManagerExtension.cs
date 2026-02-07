namespace Mars.Host.Shared.Managers.Extensions;

public static class EventManagerExtension
{
    /// <summary>
    /// <code>entity.post/{typeName}/add</code>
    /// </summary>
    public static string PostAdd(this EventManagerDefaults defaults, string typeName) => $"entity.post/{typeName}/add";

    /// <summary>
    /// <code>entity.post/{typeName}/update</code>
    /// </summary>
    public static string PostUpdate(this EventManagerDefaults defaults, string typeName) => $"entity.post/{typeName}/update";

    /// <summary>
    /// <code>entity.post/{typeName}/delete</code>
    /// </summary>
    public static string PostDelete(this EventManagerDefaults defaults, string typeName) => $"entity.post/{typeName}/delete";

    /// <summary>
    /// <code>entity.post/{typeName}/*</code>
    /// </summary>
    public static string PostAnyOperation(this EventManagerDefaults defaults, string typeName) => $"entity.post/{typeName}/*";

    /// <summary>
    /// <code>Option/{optionClassName}</code>
    /// </summary>
    public static string OptionUpdate(this EventManagerDefaults defaults, string optionClassName) => $"Option.{optionClassName}";

    /// <summary>
    /// <code>entity.PostType/{typeName}/add</code>
    /// </summary>
    public static string PostTypeAdd(this EventManagerDefaults defaults, string typeName) => $"entity.PostType/{typeName}/add";

    /// <summary>
    /// <code>entity.PostType/{typeName}/update</code>
    /// </summary>
    public static string PostTypeUpdate(this EventManagerDefaults defaults, string typeName) => $"entity.PostType/{typeName}/update";

    /// <summary>
    /// <code>entity.PostType/{typeName}/delete</code>
    /// </summary>
    public static string PostTypeDelete(this EventManagerDefaults defaults, string typeName) => $"entity.PostType/{typeName}/delete";

    /// <summary>
    /// <code>entity.PostType/{typeName}/*</code>
    /// </summary>
    public static string PostTypeAnyOperation(this EventManagerDefaults defaults, string typeName) => $"entity.PostType/{typeName}/*";

    /// <summary>
    /// <code>entity.PostType/**</code>
    /// </summary>
    public static string PostTypeAnyOperation(this EventManagerDefaults defaults) => $"entity.PostType/**";

    /// <summary>
    /// <code>entity.UserType/{typeName}/add</code>
    /// </summary>
    public static string UserTypeAdd(this EventManagerDefaults defaults, string typeName) => $"entity.UserType/{typeName}/add";

    /// <summary>
    /// <code>entity.UserType/{typeName}/update</code>
    /// </summary>
    public static string UserTypeUpdate(this EventManagerDefaults defaults, string typeName) => $"entity.UserType/{typeName}/update";

    /// <summary>
    /// <code>entity.UserType/{typeName}/delete</code>
    /// </summary>
    public static string UserTypeDelete(this EventManagerDefaults defaults, string typeName) => $"entity.UserType/{typeName}/delete";

    /// <summary>
    /// <code>entity.UserType/{typeName}/*</code>
    /// </summary>
    public static string UserTypeAnyOperation(this EventManagerDefaults defaults, string typeName) => $"entity.UserType/{typeName}/*";

    /// <summary>
    /// <code>entity.UserType/**</code>
    /// </summary>
    public static string UserTypeAnyOperation(this EventManagerDefaults defaults) => $"entity.UserType/**";

    public static string RoleAdd(this EventManagerDefaults defaults) => $"entity/role/add";
    public static string RoleUpdate(this EventManagerDefaults defaults) => $"entity/role/update";
    public static string RoleDelete(this EventManagerDefaults defaults) => $"entity/role/delete";

    public static string NavMenuAdd(this EventManagerDefaults defaults) => $"entity/navmenu/add";
    public static string NavMenuUpdate(this EventManagerDefaults defaults) => $"entity/navmenu/update";
    public static string NavMenuDelete(this EventManagerDefaults defaults) => $"entity/navmenu/delete";

    public static string FeedbackAdd(this EventManagerDefaults defaults) => $"entity/feedback/add";
    public static string FeedbackUpdate(this EventManagerDefaults defaults) => $"entity/feedback/update";
    public static string FeedbackDelete(this EventManagerDefaults defaults) => $"entity/feedback/delete";

    public static string UserAdd(this EventManagerDefaults defaults) => $"entity/user/add";
    public static string UserUpdate(this EventManagerDefaults defaults) => $"entity/user/update";
    public static string UserDelete(this EventManagerDefaults defaults) => $"entity/user/delete";

    /// <summary>
    /// <code>entity.PostCategoryType/{typeName}/add</code>
    /// </summary>
    public static string PostCategoryTypeAdd(this EventManagerDefaults defaults, string typeName) => $"entity.PostCategoryType/{typeName}/add";

    /// <summary>
    /// <code>entity.PostCategoryType/{typeName}/update</code>
    /// </summary>
    public static string PostCategoryTypeUpdate(this EventManagerDefaults defaults, string typeName) => $"entity.PostCategoryType/{typeName}/update";

    /// <summary>
    /// <code>entity.PostCategoryType/{typeName}/delete</code>
    /// </summary>
    public static string PostCategoryTypeDelete(this EventManagerDefaults defaults, string typeName) => $"entity.PostCategoryType/{typeName}/delete";

    /// <summary>
    /// <code>entity.PostCategoryType/{typeName}/*</code>
    /// </summary>
    public static string PostCategoryTypeAnyOperation(this EventManagerDefaults defaults, string typeName) => $"entity.PostCategoryType/{typeName}/*";

    /// <summary>
    /// <code>entity.PostCategoryType/**</code>
    /// </summary>
    public static string PostCategoryTypeAnyOperation(this EventManagerDefaults defaults) => $"entity.PostCategoryType/**";

    /// <summary>
    /// <code>entity.PostCategory/{typeName}/add</code>
    /// </summary>
    public static string PostCategoryAdd(this EventManagerDefaults defaults, string typeName) => $"entity.PostCategory/{typeName}/add";

    /// <summary>
    /// <code>entity.PostCategory/{typeName}/update</code>
    /// </summary>
    public static string PostCategoryUpdate(this EventManagerDefaults defaults, string typeName) => $"entity.PostCategory/{typeName}/update";

    /// <summary>
    /// <code>entity.PostCategory/{typeName}/delete</code>
    /// </summary>
    public static string PostCategoryDelete(this EventManagerDefaults defaults, string typeName) => $"entity.PostCategory/{typeName}/delete";

    /// <summary>
    /// <code>entity.PostCategory/{typeName}/*</code>
    /// </summary>
    public static string PostCategoryAnyOperation(this EventManagerDefaults defaults, string typeName) => $"entity.PostCategory/{typeName}/*";
}
