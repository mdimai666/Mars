using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using DynamicExpresso;
using Mars.Contracts.Common;
using Mars.Core.Features;
using Mars.Data.Entities;
using Mars.SiteEngine.Abstractions.Templators;
using Microsoft.EntityFrameworkCore;

namespace Mars.QueryLang.Host.Services;

public partial class EfStringQuery : IDynamicQueryableObject
{
    public const string DefaultVarName = "post";

    public IQueryable query { get; private set; }

    private readonly XInterpreter ppt;
    private readonly Dictionary<string, MethodInfo> map;

    public EfStringQuery(IQueryable query, XInterpreter ppt)
    {
        this.query = query;
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
            return Union((IQueryable)args![0]!);
        }
        else if (map.TryGetValue(methodName, out var method))
        {
            return method.Invoke(this, args);
        }
        throw new NotImplementedException($"'{methodName}' Not Implemented. For '{args}'");
    }

    #region Expression normalization

    // Поддерживаются три формы: bare `Title == "x"`, легаси `post.Title == "x"`, lambda `p => p.Title == "x"`.
    // Bare распознаётся по первому идентификатору: если это член элементного типа — подставляется DefaultVarName;
    // иначе выражение парсится как есть (переменные интерпретатора, статические вызовы и т.п.).
    (string varName, string body) NormalizeExpression(string exp)
    {
        var arrowIndex = exp.IndexOf("=>", StringComparison.Ordinal);
        if (arrowIndex > 0)
        {
            var left = exp[..arrowIndex].Trim();
            if (FullIdentifierRegex().IsMatch(left))
            {
                return (left, exp[(arrowIndex + 2)..].Trim());
            }
        }

        var body = exp.Trim();
        var leading = LeadingIdentifierRegex().Match(body);
        if (leading.Success)
        {
            var name = leading.Groups[1].Value;
            if (name != DefaultVarName
                && query.ElementType.GetMember(name, BindingFlags.Public | BindingFlags.Instance).Length > 0)
            {
                body = DefaultVarName + "." + body;
            }
        }

        return (DefaultVarName, body);
    }

    LambdaExpression ParsePredicate(string exp)
    {
        var (varName, body) = NormalizeExpression(exp);

        var delegateType = typeof(Func<,>).MakeGenericType(query.ElementType, typeof(bool));
        return (LambdaExpression)ParseAsExpressionDefinition
            .MakeGenericMethod(delegateType)
            .Invoke(ppt.Get, [body, new[] { varName }])!;
    }

    LambdaExpression ParseKeySelector(string exp)
    {
        var (varName, body) = NormalizeExpression(exp);

        var prefix = varName + ".";
        if (body.StartsWith(prefix, StringComparison.Ordinal))
        {
            body = body[prefix.Length..];
        }

        var parameter = Expression.Parameter(query.ElementType, varName);
        Expression accessor = parameter;
        foreach (var segment in body.Split('.'))
        {
            accessor = Expression.PropertyOrField(accessor, segment.Trim());
        }

        return Expression.Lambda(accessor, parameter);
    }

    #endregion

    #region Queryable reflection

    static readonly MethodInfo ParseAsExpressionDefinition = typeof(Interpreter)
        .GetMethod(nameof(Interpreter.ParseAsExpression), BindingFlags.Public | BindingFlags.Instance)!;

    static readonly MethodInfo IncludeStringDefinition = typeof(EntityFrameworkQueryableExtensions)
        .GetMethods(BindingFlags.Static | BindingFlags.Public)
        .First(mi => mi.Name == nameof(EntityFrameworkQueryableExtensions.Include)
                   && mi.IsGenericMethodDefinition
                   && mi.GetParameters().Length == 2
                   && mi.GetParameters()[1].ParameterType == typeof(string));

    static readonly MethodInfo ToListDefinition = typeof(Enumerable)
        .GetMethods(BindingFlags.Static | BindingFlags.Public)
        .First(mi => mi.Name == nameof(Enumerable.ToList)
                   && mi.IsGenericMethodDefinition
                   && mi.GetParameters().Length == 1);

    static IList ToListMaterialized(IQueryable source) =>
        (IList)ToListDefinition.MakeGenericMethod(source.ElementType).Invoke(null, [source])!;

    static readonly ConcurrentDictionary<(string name, int paramsCount, string? secondParamName), MethodInfo> QueryableMethods = new();

    static MethodInfo FindQueryableMethod(string name, int paramsCount, string? secondParamName = null) =>
        QueryableMethods.GetOrAdd((name, paramsCount, secondParamName), key =>
            typeof(Queryable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(mi => mi.Name == key.name
                             // this check technically not required, but more future proof
                             && mi.IsGenericMethodDefinition
                             && mi.GetParameters().Length == key.paramsCount
                             && (key.secondParamName is null || mi.GetParameters()[1].Name == key.secondParamName)));

    static object? QCall(IQueryable source, string methodName, int paramsCount, string? secondParamName, params object?[] args)
    {
        var method = FindQueryableMethod(methodName, paramsCount, secondParamName)
            .MakeGenericMethod(source.ElementType);
        return method.Invoke(null, [source, .. args]);
    }

    object? Call(string methodName, int paramsCount, string? secondParamName, params object?[] args) =>
        QCall(query, methodName, paramsCount, secondParamName, args);

    object? CallPredicate(string methodName, string expr) =>
        Call(methodName, 2, "predicate", ParsePredicate(expr));

    object? CallKeySelector(string methodName, string exp, string paramName)
    {
        var selector = ParseKeySelector(exp);
        var method = FindQueryableMethod(methodName, 2, paramName)
            .MakeGenericMethod(query.ElementType, selector.ReturnType);
        return method.Invoke(null, [query, selector]);
    }

    #endregion

    public Dictionary<string, MethodInfo> MethodsMapping()
    {
        var methods = GetType()
              .GetMethods(BindingFlags.Instance | BindingFlags.Public)
              .Where(mi => mi.GetParameters().Length == 1
                         && mi.GetParameters()[0].ParameterType == typeof(string))
              .Concat([
                  GetType().GetMethod(nameof(Union))!
                ]);

        return methods.ToDictionary(s => s.Name)!;
    }

    [TemplatorHelperInfo("Count", """.Count(@expr?)""", "Возвращает количество элементов в запросе. @expr необязательно — условие фильтрации (формы как у Where).")]
    public int Count(string expr = "")
    {
        if (string.IsNullOrEmpty(expr))
            return (int)Call(nameof(Queryable.Count), 1, null)!;

        return (int)CallPredicate(nameof(Queryable.Count), expr)!;
    }

    [TemplatorHelperInfo("First", """.First(@expr?)""", "Возвращает первый элемент; запрос не изменяет. @expr необязательно — условие фильтрации (формы как у Where).")]
    public object? First(string expr = "")
    {
        if (string.IsNullOrEmpty(expr))
            return Call(nameof(Queryable.FirstOrDefault), 1, null);

        return CallPredicate(nameof(Queryable.FirstOrDefault), expr);
    }

    [TemplatorHelperInfo("Last", """.Last(@expr?)""", "Возвращает последний элемент; запрос не изменяет. Требует предварительной сортировки OrderBy. @expr необязательно — условие фильтрации.")]
    public object? Last(string expr = "")
    {
        if (string.IsNullOrEmpty(expr))
            return Call(nameof(Queryable.LastOrDefault), 1, null);

        return CallPredicate(nameof(Queryable.LastOrDefault), expr);
    }

    [TemplatorHelperInfo("OrderBy", """OrderBy(@fieldName)""", "Сортирует элементы по указанному полю. @fieldName — имя поля или путь через точку (User.Name).")]
    public EfStringQuery OrderBy(string fieldName)
    {
        query = (IQueryable)CallKeySelector(nameof(Queryable.OrderBy), fieldName, "keySelector")!;
        return this;
    }

    [TemplatorHelperInfo("OrderByDescending", """OrderByDescending(@fieldName)""", "Сортирует элементы по указанному полю в порядке убывания. @fieldName — имя поля или путь через точку (User.Name).")]
    public EfStringQuery OrderByDescending(string fieldName)
    {
        query = (IQueryable)CallKeySelector(nameof(Queryable.OrderByDescending), fieldName, "keySelector")!;
        return this;
    }

    [TemplatorHelperInfo("ThenBy", """ThenBy(@fieldName)""", "Продолжает сортировку элементов по указанному полю. @fieldName — имя поля или путь через точку (User.Name).")]
    public EfStringQuery ThenBy(string fieldName)
    {
        query = (IQueryable)CallKeySelector(nameof(Queryable.ThenBy), fieldName, "keySelector")!;
        return this;
    }

    [TemplatorHelperInfo("ThenByDescending", """ThenByDescending(@fieldName)""", "Продолжает сортировку элементов по указанному полю в порядке убывания. @fieldName — имя поля или путь через точку (User.Name).")]
    public EfStringQuery ThenByDescending(string fieldName)
    {
        query = (IQueryable)CallKeySelector(nameof(Queryable.ThenByDescending), fieldName, "keySelector")!;
        return this;
    }

    [TemplatorHelperInfo("Skip", """Skip(@count)""", "Пропускает указанное количество элементов. @count - количество элементов для пропуска")]
    public EfStringQuery Skip(int count)
    {
        query = (IQueryable)Call(nameof(Queryable.Skip), 2, null, count)!;
        return this;
    }

    [TemplatorHelperInfo("Skip", """Skip(@expr)""", "Пропускает указанное количество элементов. @expr - выражение для вычисления количества элементов для пропуска")]
    public EfStringQuery Skip(string expr)
    {
        return Skip(ppt.Get.Eval<int>(expr));
    }

    [TemplatorHelperInfo("Take", """Take(@count)""", "Ограничивает количество элементов в запросе. @count - количество элементов для ограничения")]
    public EfStringQuery Take(int count)
    {
        query = (IQueryable)Call(nameof(Queryable.Take), 2, null, count)!;
        return this;
    }

    [TemplatorHelperInfo("Take", """Take(@expr)""", "Ограничивает количество элементов в запросе. @expr - выражение для вычисления количества элементов для ограничения")]
    public EfStringQuery Take(string expr)
    {
        return Take(ppt.Get.Eval<int>(expr));
    }

    [TemplatorHelperInfo("Where", """Where(@expr)""", "Фильтрует элементы по указанному выражению. Формы @expr: Title == \"x\" (поле), p => p.Title == \"x\" (лямбда), post.Title == \"x\" (старая форма).")]
    public EfStringQuery Where(string expr)
    {
        string _expr = expr;

        if (_expr.StartsWith('='))
        {
            _expr = ppt.Get.Eval<string>(expr[1..]);
        }

        query = (IQueryable)CallPredicate(nameof(Queryable.Where), _expr)!;

        return this;
    }

    [TemplatorHelperInfo("ToList", """ToList()""", "Преобразует запрос в список.")]
    public IEnumerable ToList(string expr = "")
    {
        return ToListMaterialized(query);
    }

    [TemplatorHelperInfo("Select", """Select(@expr)""", "Проецирует элементы в поле. @expr — имя поля или путь через точку (User.Name); последующие методы применяются уже к проекции.")]
    public object Select(string expr)
    {
        // проекция становится текущим запросом: элементный тип меняется,
        // поэтому авто-ToList в handler'е материализует именно её
        query = (IQueryable)CallKeySelector(nameof(Queryable.Select), expr, "selector")!;

        return query;
    }

    [TemplatorHelperInfo("Include", """Include(@expr)""", "Включает связанные данные в запрос. @expr - имя навигационного свойства или список свойств через запятую")]
    public EfStringQuery Include(string expr)
    {
        var navigationPropertyArg = ppt.Get.Eval<string>(expr);

        var method = IncludeStringDefinition.MakeGenericMethod(query.ElementType);

        foreach (var navigationProperty in navigationPropertyArg.Split(','))
        {
            query = (IQueryable)method.Invoke(null, [query, navigationProperty.Trim()])!;
        }

        return this;
    }

    [TemplatorHelperInfo("Table", """Table(@page, @size)""", "Пагинация. Возвращает элементы в виде таблицы с пагинацией. @page - номер страницы, @size - количество элементов на странице")]
    public object Table(string expr)
    {
        var args = TextHelper.SplitArguments(expr);
        MyThrowHelper.IfArgumentCount(args, 2, "arguments require 2");

        int page = ppt.Get.Eval<int>(args[0], ppt.GetParameters());
        int size = ppt.Get.Eval<int>(args[1], ppt.GetParameters());

        var source = query;
        var filter = BasicListQuery.FromPage(page, size);

        var paged = (IQueryable)QCall(source, nameof(Queryable.Skip), 2, null, filter.Skip)!;
        paged = (IQueryable)QCall(paged, nameof(Queryable.Take), 2, null, filter.Take)!;

        var items = ToListMaterialized(paged);
        int totalCount = (int)QCall(source, nameof(Queryable.Count), 1, null)!;

        var elementType = source.ElementType;
        var array = Array.CreateInstance(elementType, items.Count);
        items.CopyTo(array, 0);

        query = array.AsQueryable();

        return Activator.CreateInstance(
            typeof(TotalResponse2<>).MakeGenericType(elementType),
            array, page, size, totalCount > items.Count, (int?)totalCount)!;
    }

    [TemplatorHelperInfo("Search", """Search(@searchText)""", "Поиск по тексту. @searchText - текст для поиска")]
    public EfStringQuery Search(string searchText)
    {
        var _searchText = ppt.Get.Eval<string>(searchText).ToLower();

        // IQueryable<out T> ковариантен: пропускает и производные от PostEntity (Mto-модели)
        if (query is not IQueryable<PostEntity> posts)
        {
            throw new NotImplementedException();
        }

        var pattern = $"%{_searchText}%";
        query = posts.Where(s => EF.Functions.ILike(s.Title, pattern) || EF.Functions.ILike(s.Content!, pattern));

        return this;
    }

    [TemplatorHelperInfo("Union", """Union(@secondQueryable)""", "Объединяет текущий запрос с другим запросом. @secondQueryable - второй запрос для объединения")]
    public EfStringQuery Union(IQueryable secondQueryable)
    {
        query = (IQueryable)Call(nameof(Queryable.Union), 2, null, secondQueryable)!;
        return this;
    }

    public IQueryable GetQuery() => query;

    [GeneratedRegex("^[A-Za-z_]\\w*$")]
    private static partial Regex FullIdentifierRegex();

    [GeneratedRegex("^([A-Za-z_]\\w*)")]
    private static partial Regex LeadingIdentifierRegex();
}

public class TotalResponse2<T> : PagingResult<T>
{
    public TotalResponse2(IReadOnlyCollection<T> items, int page, int pageSize, bool hasMoreData, int? totalCount = null) : base(items, page, pageSize, hasMoreData, totalCount)
    {
        Paginator = new(page, totalCount ?? items.Count, pageSize);
    }

    public PaginatorHelper Paginator { get; }
}
