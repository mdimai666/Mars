using System.Reflection;
using FluentAssertions;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.WebSite.Models;
using Mars.Host.Templators;
using Mars.QueryLang.Host.Services;
using Mars.Shared.Options;
using Mars.Shared.Templators;
using Mars.Test.Common.Constants;
using Mars.WebSiteProcessor.Handlebars.TemplateData;
using NSubstitute;

namespace Test.Mars.WebSiteProcessor.QueryLangProcessings;

public class QueryLangProcessingTests
{
    private readonly TemplatorFeaturesLocator tfLocator;
    private readonly IServiceProvider _serviceProvider;
    private readonly IQueryLangLinqDatabaseQueryHandler _linqDatabaseQueryHandler;

    private readonly QueryLangProcessing _handler;
    private readonly PageRenderContext _pageContext;

    public QueryLangProcessingTests()
    {
        var sysOptions = new SysOptions() { SiteUrl = "http://localhost" };
        _pageContext = new PageRenderContext()
        {
            Request = new WebClientRequest(new Uri(sysOptions.SiteUrl)),
            SysOptions = sysOptions,
            User = new RenderContextUser(UserConstants.TestUser),
            RenderParam = new RenderParam(),
            IsDevelopment = true,
        };
        var dataFiller = new HandlebarsTmpCtxBasicDataContext();
        dataFiller.FillTemplateDictionary(_pageContext, _pageContext.TemplateContextVaribles);

        tfLocator = new TemplatorFeaturesLocator();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _linqDatabaseQueryHandler = Substitute.For<IQueryLangLinqDatabaseQueryHandler>();

        _handler = new QueryLangProcessing(tfLocator, _serviceProvider, _linqDatabaseQueryHandler);
    }

    [Fact]
    public async Task Process_PrimitiveQuery_Success()
    {
        // Arrange
        var queries = new Dictionary<string, string>()
        {
            ["x"] = "=1+1",
        };

        // Act
        var result = await _handler.Process(_pageContext, queries, null, default);

        // Assert
        result["x"].Should().Be(2);
    }

    [Fact]
    public async Task Process_PrimitiveAccessForNextRequest_Success()
    {
        // Arrange
        var queries = new Dictionary<string, string>()
        {
            ["x"] = "=1+1",
            ["y"] = "=x*2"
        };

        // Act
        var result = await _handler.Process(_pageContext, queries, null, default);

        // Assert
        result["y"].Should().Be(4);
    }

    [Fact]
    public async Task Process_PageContextAccess_Success()
    {
        // Arrange
        var queries = new Dictionary<string, string>()
        {
            ["x"] = "=_user.FullName",
            ["y"] = "=_req." + nameof(WebClientRequest.Host),
        };

        // Act
        var result = await _handler.Process(_pageContext, queries, null, default);

        // Assert
        result["x"].Should().Be(UserConstants.TestUser.FullName);
        result["y"].ToString().Should().Be("localhost");
    }

    [Fact]
    public async Task Process_FunctionCall_Success()
    {
        // Arrange
        var paginator = new PaginatorHelper(1, 100, 20);
        var queries = new Dictionary<string, string>()
        {
            ["x"] = nameof(TemplatorRegisterFunctions.Paginator) + "(1,100,20)",
        };
        tfLocator.Functions.Add(nameof(TemplatorRegisterFunctions.Paginator), TemplatorRegisterFunctions.Paginator);

        // Act
        var result = await _handler.Process(_pageContext, queries, null, default);

        // Assert
        var pgResult = result["x"] as PaginatorHelper;
        pgResult.Should().BeEquivalentTo(paginator, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<PaginatorHelper>()
            .ExcludingMissingMembers());

    }

    [Fact]
    public async Task Process_LinqDatabaseQuery_Success()
    {
        // Arrange
        _ = nameof(QueryLangLinqDatabaseQueryHandler.Handle);
        var queries = new Dictionary<string, string>()
        {
            ["x"] = "ef.Post.Count()",
            //["x"] = "post.Where(x=>x.Title==\"1\").List()",
        };
        _linqDatabaseQueryHandler
            .Handle(queries["x"].Substring(3), Arg.Any<XInterpreter>(), default)
            .Returns(2);

        // Act
        var result = await _handler.Process(_pageContext, queries, null, default);

        // Assert
        var pgResult = (int)result["x"]!;
        pgResult.Should().Be(2);

    }

#if Experiments
    [Fact]
    public void GoLinq1()
    {
        Type[] coreTypes = [typeof(Queryable), typeof(Enumerable)];

        //Dictionary<string, MethodInfo> dict = new();
        List<MethodPrimalSignature> list = new(400);

        var query = (new List<int> { 1, 2, 3, 4, 5 }).AsQueryable();

        foreach (var type in coreTypes)
        {
            var methods = type
                  .GetMethods(BindingFlags.Static | BindingFlags.Public);
            //.Where(mi => mi.GetParameters().Length == 1
            //           && mi.GetParameters()[0].ParameterType == typeof(string));

            list.AddRange(methods.Select(mi => new MethodPrimalSignature
            {
                IsQueryable = mi.ReturnType is IQueryable,
                //IsGeneric = true,
                ArgumentCount = mi.GetParameters().Length - 1,
                Name = mi.Name,
                IsGenericMethodDefinition = mi.IsGenericMethodDefinition,
                IsFirstArgumentExpression = mi.GetParameters().Length < 2 ? false : ((mi.GetParameters()[0] is Expression)),
                MethodInfo = mi,
            }));
        }

        var z = list.Where(s => s.Name == "Where").ToList();

        var method1 = list.Where(s => s.Name == "Where" && s.ArgumentCount == 1).FirstOrDefault();

        var ppt = new XInterpreter();

        LambdaExpression exp = ppt.Get.ParseAsExpression<Func<int, bool>>("post>2", "post");

        var w = exp.Compile();
        var t = w.DynamicInvoke(3);

        object? result = method1.MethodInfo.Invoke(query, new object[] { query, exp });

    }
#endif
}

public class MethodPrimalSignature()
{
    public required bool IsQueryable { get; init; }
    public bool IsGenericMethodDefinition { get; init; }
    public required int ArgumentCount { get; init; }
    public required string Name { get; init; }
    public required MethodInfo MethodInfo { get; init; }

    public bool IsFirstArgumentExpression { get; init; }
}
