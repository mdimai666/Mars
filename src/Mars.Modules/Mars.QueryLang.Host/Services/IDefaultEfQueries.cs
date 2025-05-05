namespace Mars.Host.QueryLang;

public interface IDefaultEfQueries<T>: IQueryable<T>, IDynamicEfQuery
{
    //IQueryable<T> Where(IQueryable<T> query, string expr);
    //IQueryable<T> OrderBy(IQueryable<T> query, string expr);
    //IQueryable<T> Take(IQueryable<T> query, int count);
    //IQueryable<T> Skip(IQueryable<T> query, int count);
    //T First(IQueryable<T> query, string expr = "");
    //T Last(IQueryable<T> query, string expr = "");
    //int Count(IQueryable<T> query, string expr = "");

    IDefaultEfQueries<T> Where(string expr);
    IDefaultEfQueries<T> OrderBy(string fieldName);
    IDefaultEfQueries<T> OrderByDescending(string fieldName);
    IDefaultEfQueries<T> Take(int count);
    IDefaultEfQueries<T> Skip(int count);
    T? First(string expr = "");
    new T? Last(string expr = "");
    new int Count(string expr = "");

    //double Average
    //bool Contains
    //IQueryable<TSource> DistinctBy
    //IQueryable<TSource> ExceptBy
    //IQueryable<TSource> Intersect
    //IQueryable<TResult> GroupBy
    //TSource ElementAt
    //TSource MaxBy
    //TSource MinBy

    //IOrderedQueryable<TSource> OrderBy
    //IOrderedQueryable<TSource> OrderByDescending
    //float Sum
}

public interface IDynamicEfQuery
{
    object? Last(string expr = "");
    int Count(string expr = "");
}
