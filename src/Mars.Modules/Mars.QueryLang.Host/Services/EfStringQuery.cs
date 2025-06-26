using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Mars.Core.Extensions;
using Mars.Core.Features;
using Mars.Host.Data.Entities;
using Mars.Host.QueryLang;
using Mars.Host.Shared.Templators;
using Mars.Shared.Common;
using Mars.Shared.Templators;
using Microsoft.EntityFrameworkCore;

namespace Mars.QueryLang.Host.Services;

public class EfStringQuery<T> : IDefaultEfQueries<T>, IDynamicQueryableObject
{
    //public EfDynQueryDict.DQC_Context ctx;
    public IQueryable<T> query;

    public IQueryable<T> baseQuery;

    public Type ElementType => query.ElementType;
    public Expression Expression => query.Expression;
    public IQueryProvider Provider => query.Provider;

    private readonly XInterpreter ppt;
    private readonly Dictionary<string, MethodInfo> map;

    Type? _currentQueryEntityType = null;
    Type CurrentQueryEntityType
    {
        get => _currentQueryEntityType ?? baseQuery.ElementType;
    }

    protected bool CurrentQueriGetSingle = false;
    protected bool CurrentQueriGetTable = false;

    protected TotalResponse2<T>? _tableResponse;

    public EfStringQuery(IQueryable<T> query, XInterpreter ppt)
    {
        this.query = query;
        baseQuery = query;
        this.ppt = ppt;

        map = MethodsMapping();
    }

    public object? InvokeMethod(string methodName, string expr)
    {
        if (map.TryGetValue(methodName, out var method))
        {
            return method.Invoke(this, [expr]);
        }
        throw new NotImplementedException($"'{methodName}' Not Implemented. For '{expr}'");
    }

    public object? InvokeMethodArgs(string methodName, object?[]? args)
    {
        if (methodName == nameof(Union))
        {
            return Union((args[0] as IQueryable<T>)!);
        }
        else if (map.TryGetValue(methodName, out var method))
        {
            return method.Invoke(this, args);
        }
        throw new NotImplementedException($"'{methodName}' Not Implemented. For '{args}'");
    }

    public Expression<Func<T, bool>> ParseExp(string exp, string varName = "post")
    {
        return ppt.Get.ParseAsExpression<Func<T, bool>>(exp, varName);
    }

    public Expression<Func<TSource, TKey>> ParseKeySelector<TSource, TKey>(string exp, string varName = "post")
    {
        return ppt.Get.ParseAsExpression<Func<TSource, TKey>>(exp, varName);
    }

    LambdaExpression ParseExpA(string exp, string varName = "post")
    {
        MethodInfo method = GetType().GetMethod(nameof(this.ParseExp), BindingFlags.Public | BindingFlags.Instance, new Type[] { typeof(string), typeof(string) })!;
        //method = method.MakeGenericMethod(typeof(T));
        return (method.Invoke(this, new[] { exp, varName }) as LambdaExpression)!;
    }

    LambdaExpression ParseKeySelectorA(string propertyOrFieldName, string varName = "post")
    {
        var elementType = CurrentQueryEntityType;
        var parameterExpression = Expression.Parameter(elementType);
        var propertyOrFieldExpression = Expression.PropertyOrField(parameterExpression, propertyOrFieldName);

        var selector = Expression.Lambda(propertyOrFieldExpression, parameterExpression);

        return selector;
    }

    object? CallQueryableMethod(string methodName, string expr)
    {
        var exp = ParseExpA(expr);

        MethodInfo method = typeof(Queryable)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
              .First(mi => mi.Name == methodName
                         // this check technically not required, but more future proof
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 2
                         && mi.GetParameters()[1].Name == "predicate")
              .MakeGenericMethod(CurrentQueryEntityType);

        object? result = method.Invoke(query, new object[] { query, exp });

        return result;
    }

    public Dictionary<string, MethodInfo> MethodsMapping()
    {
        var methods = GetType()
              .GetMethods(BindingFlags.Instance | BindingFlags.Public)
              .Where(mi => mi.GetParameters().Length == 1
                         && mi.GetParameters()[0].ParameterType == typeof(string))
              .Concat([
                  GetType().GetMethod(nameof(Union))
                ]);

        return methods.ToDictionary(s => s.Name)!;
    }

    [TemplatorHelperInfo("Count", """.Count(@expr?)""", "Возвращает количество элементов в запросе. Если @expr не указано, то возвращает общее количество элементов в запросе")]
    public int Count(string expr = "")
    {
        if (string.IsNullOrEmpty(expr))
            return query.Count();

        var result = CallQueryableMethod(nameof(Queryable.Count), expr) as int?;

        return result!.Value;

    }

    [TemplatorHelperInfo("First", """.First(@expr?)""", "Возвращает первый элемент. Если @expr указано, то возвращает применяет выборку.")]
    public T? First(string expr = "")
    {
        CurrentQueriGetSingle = true;
        if (string.IsNullOrEmpty(expr))
        {
            Take(1);
            return query.FirstOrDefault();
        }

        //var exp = ParseExp<IBasicEntity>(expr);
        //return query.First(exp);

        var result = CallQueryableMethod(nameof(Queryable.FirstOrDefault), expr);
        Where(expr);
        Take(1);

        return (T?)result;
    }

    [TemplatorHelperInfo("Last", """.Last(@expr?)""", "Возвращает последний элемент. Если @expr указано, то возвращает применяет выборку.")]
    public T? Last(string expr = "")
    {
        CurrentQueriGetSingle = true;
        if (string.IsNullOrEmpty(expr))
        {
            query = query.Reverse().Take(1);
            //Take(1);
            return query.LastOrDefault();
        }

        //var exp = ParseExp<IBasicEntity>(expr);
        //return query.Last(exp);

        var result = CallQueryableMethod(nameof(Queryable.LastOrDefault), expr);
        Where(expr);
        query = query.Reverse().Take(1);
        //query = Take(1);

        return (T?)result!;
    }

    object? CallQueryableKeySelMethod(string methodName, string fieldName, string paramName)
    {
        var exp = ParseKeySelectorA(fieldName);

        MethodInfo method = typeof(Queryable)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
              .First(mi => mi.Name == methodName
                         // this check technically not required, but more future proof
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 2
                         && mi.GetParameters()[1].Name == paramName)
              .MakeGenericMethod(CurrentQueryEntityType, exp.ReturnType);

        object? result = method.Invoke(query, new object[] { query, exp });

        return result;
    }

    [TemplatorHelperInfo("OrderBy", """.OrderBy(@fieldName)""", "Сортирует элементы по указанному полю. @fieldName - имя поля для сортировки")]
    public IDefaultEfQueries<T> OrderBy(string fieldName)
    {
        var result = CallQueryableKeySelMethod(nameof(Queryable.OrderBy), fieldName, "keySelector");

        query = (result as IQueryable<T>)!;

        return this;
    }

    [TemplatorHelperInfo("OrderByDescending", """.OrderByDescending(@fieldName)""", "Сортирует элементы по указанному полю в порядке убывания. @fieldName - имя поля для сортировки")]
    public IDefaultEfQueries<T> OrderByDescending(string fieldName)
    {
        var result = CallQueryableKeySelMethod(nameof(Queryable.OrderByDescending), fieldName, "keySelector");

        query = (result as IQueryable<T>)!;

        return this;
    }

    [TemplatorHelperInfo("ThenBy", """.ThenBy(@fieldName)""", "Продолжает сортировку элементов по указанному полю. @fieldName - имя поля для сортировки")]
    public IDefaultEfQueries<T> ThenBy(string fieldName)
    {
        var result = CallQueryableKeySelMethod(nameof(Queryable.ThenBy), fieldName, "keySelector");

        query = (result as IQueryable<T>)!;

        return this;
    }

    [TemplatorHelperInfo("ThenByDescending", """.ThenByDescending(@fieldName)""", "Продолжает сортировку элементов по указанному полю в порядке убывания. @fieldName - имя поля для сортировки")]
    public IDefaultEfQueries<T> ThenByDescending(string fieldName)
    {
        var result = CallQueryableKeySelMethod(nameof(Queryable.ThenByDescending), fieldName, "keySelector");

        query = (result as IQueryable<T>)!;

        return this;
    }

    [TemplatorHelperInfo("Skip", """.Skip(@count)""", "Пропускает указанное количество элементов. @count - количество элементов для пропуска")]
    public IDefaultEfQueries<T> Skip(int count)
    {
        query = query.Skip(count);
        return this;
    }

    [TemplatorHelperInfo("Skip", """.Skip(@expr)""", "Пропускает указанное количество элементов. @expr - выражение для вычисления количества элементов для пропуска")]
    public IDefaultEfQueries<T> Skip(string expr)
    {
        int count = ppt.Get.Eval<int>(expr);
        query = query.Skip(count);
        return this;
    }

    [TemplatorHelperInfo("Take", """.Take(@count)""", "Ограничивает количество элементов в запросе. @count - количество элементов для ограничения")]
    public IDefaultEfQueries<T> Take(int count)
    {
        query = query.Take(count);
        return this;
    }

    [TemplatorHelperInfo("Take", """.Take(@expr)""", "Ограничивает количество элементов в запросе. @expr - выражение для вычисления количества элементов для ограничения")]
    public IDefaultEfQueries<T> Take(string expr)
    {
        int count = ppt.Get.Eval<int>(expr);
        query = query.Take(count);
        return this;
    }

    [TemplatorHelperInfo("Where", """.Where(@expr)""", "Фильтрует элементы по указанному выражению. @expr - выражение для фильтрации")]
    public IDefaultEfQueries<T> Where(string expr)
    {
        //var exp = ParseExp<IBasicEntity>(expr);
        //return query.Where(exp);

        string _expr = expr;

        if (_expr.StartsWith('='))
        {
            var a = expr.Substring(1, expr.Length - 1);
            _expr = ppt.Get.Eval<string>(a);
        }

        var result = CallQueryableMethod(nameof(Queryable.Where), _expr);

        query = (result as IQueryable<T>)!;

        return this;
    }

    [TemplatorHelperInfo("ToList", """.ToList()""", "Преобразует запрос в список.")]
    public IEnumerable ToList(string expr)
    {
        return query.ToList();
    }

    [TemplatorHelperInfo("Select", """.Select(@expr)""", "Выбирает элементы из запроса по указанному выражению. @expr - выражение для выбора элементов")]
    public object Select(string expr)
    {
        //var exp = ParseExpA(expr);

        var fieldName = expr;

        var result = CallQueryableKeySelMethod(nameof(Queryable.Select), fieldName, "selector");

        //query = result as IQueryable<IBasicEntity>;

        return result!;
    }

    [TemplatorHelperInfo("Include", """.Include(@expr)""", "Включает связанные данные в запрос. @expr - имя навигационного свойства или список свойств через запятую")]
    public IDefaultEfQueries<T> Include(string expr)
    {
        //var exp = ParseExpA(expr);

        var navigationPropertyArg = ppt.Get.Eval<string>(expr);

        var methodName = nameof(EntityFrameworkQueryableExtensions.Include);

        MethodInfo method = typeof(EntityFrameworkQueryableExtensions)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
              .First(mi => mi.Name == methodName
                         // this check technically not required, but more future proof
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 2
                         && mi.GetParameters()[1].ParameterType == typeof(string))
              .MakeGenericMethod(baseQuery.ElementType);

        foreach (var navigationProperty in navigationPropertyArg.Split(','))
        {
            object? result = method.Invoke(query, new object[] { query, navigationProperty.Trim() });
            query = (result as IQueryable<T>)!;
        }

        return this;
    }

    [TemplatorHelperInfo("Table", """.Table(@page, @size)""", "Пагинация. Возвращает элементы в виде таблицы с пагинацией. @page - номер страницы, @size - количество элементов на странице")]
    public TotalResponse2<T> Table(string expr)
    {
        var args = TextHelper.SplitArguments(expr);
        MyThrowHelper.IfArgumentCount(args, 2, "arguments require 2");

        var z = ppt.GetParameters();

        int page = ppt.Get.Eval<int>(args[0], ppt.GetParameters());
        int size = ppt.Get.Eval<int>(args[1], ppt.GetParameters());

        var source = query;
        var filter = BasicListQuery.FromPage(page, size);

        var items = source.Skip(filter.Skip).Take(filter.Take).ToList();
        int totalCount = source.Count();

        CurrentQueriGetTable = true;
        query = items.AsQueryable();

        return _tableResponse = new TotalResponse2<T>(items, page, size, totalCount > items.Count, totalCount);
    }

    [TemplatorHelperInfo("Search", """.Search(@searchText)""", "Поиск по тексту. @searchText - текст для поиска")]
    public IDefaultEfQueries<T> Search(string searchText)
    {
        //var exp = ParseExp<IBasicEntity>(expr);
        //return query.Where(exp);

        //var result = CallQueryableMethod(nameof(Queryable.Where), );

        IQueryable<T> result;
        var _searchText = ppt.Get.Eval<string>(searchText).ToLower();

        if (typeof(PostEntity).IsAssignableFrom(CurrentQueryEntityType))
        {
            var pattern = $"%{_searchText}%";
            result = ((query as IQueryable<PostEntity>)!
                .Where(s => EF.Functions.ILike(s.Title, pattern) || EF.Functions.ILike(s.Content!, _searchText))
                as IQueryable<T>)!;
        }
        else
        {
            throw new NotImplementedException();
            //result = query.Where(s => s.Id.ToString().Contains(_searchText));
        }

        query = result;

        return this;
    }

    [TemplatorHelperInfo("Union", """.Union(@secondQueryable)""", "Объединяет текущий запрос с другим запросом. @secondQueryable - второй запрос для объединения")]
    public IDefaultEfQueries<T> Union(IQueryable<T> secondQueryable)
    {
        query = query.Union(secondQueryable);
        return this;
    }

    public IQueryable GetQuery() => query;

    public IEnumerator<T> GetEnumerator()
    {
        return query?.GetEnumerator()!;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return (query as IEnumerable).GetEnumerator();
    }

    object? IDynamicEfQuery.Last(string expr) => Last(expr);
}

public class TotalResponse2<T> : PagingResult<T>
{
    public TotalResponse2(IReadOnlyCollection<T> items, int page, int pageSize, bool hasMoreData, int? totalCount = null) : base(items, page, pageSize, hasMoreData, totalCount)
    {
        Paginator = new(page, totalCount ?? items.Count, pageSize);
    }

    public PaginatorHelper Paginator { get; }
}
