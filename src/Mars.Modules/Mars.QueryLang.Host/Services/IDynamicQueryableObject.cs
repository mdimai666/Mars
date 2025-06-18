namespace Mars.QueryLang.Host.Services;

public interface IDynamicQueryableObject
{
    object? InvokeMethod(string methodName, string expr);
    object? InvokeMethodArgs(string methodName, object?[]? args);
    IQueryable GetQuery();
}
