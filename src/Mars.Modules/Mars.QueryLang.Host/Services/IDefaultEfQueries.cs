namespace Mars.QueryLang.Host.Services;

public interface IDefaultEfQueries<T> : IQueryable<T>, IDynamicEfQuery
{
    IDefaultEfQueries<T> Where(string expr);
    IDefaultEfQueries<T> OrderBy(string fieldName);
    IDefaultEfQueries<T> OrderByDescending(string fieldName);
    IDefaultEfQueries<T> Take(int count);
    IDefaultEfQueries<T> Skip(int count);
    T? First(string expr = "");
    new T? Last(string expr = "");
    new int Count(string expr = "");
}

public interface IDynamicEfQuery
{
    object? Last(string expr = "");
    int Count(string expr = "");
}
