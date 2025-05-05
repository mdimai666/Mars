using Mars.GenSourceCode;
using System;
using Mars.Host.Data;
using Mars.Host.QueryLang;
using Microsoft.Extensions.DependencyInjection;
using Mars.Host.Services;
using Microsoft.EntityFrameworkCore;
using AppShared.Models;
using System.Reflection;
using System.Linq.Expressions;
using static Test.Mars.Host.SomeTests.WorkAboutSelectExpression;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Test.Mars.Host.TestHostApp;
using Test.Mars.Host.Models;
using Mars.Host.Shared.Services;

namespace Test.Mars.Host.RuntimeTypeCompilerTests;

public class RuntimeTypeCompilerTests : UnitTestHostBaseClass
{
    [Fact]
    public void TestCompile()
    {

        using var ef = _serviceProvider.GetService<MarsDbContextLegacy>();

        var postTypeList = ef.PostTypes
                                .Include(s => s.MetaFields)
                                //.First(s => s.TypeName == postTypeName);
                                .AsNoTracking()
                                .ToList();

        var userService = _serviceProvider.GetService<UserService>();
        var userMetaFields = userService.UserMetaFields(ef);

        IMetaModelTypesLocator mlocator = _serviceProvider.GetRequiredService<IMetaModelTypesLocator>();
        RuntimeTypeCompiler compiler = new();
        var result = compiler.Compile(postTypeList, userMetaFields, mlocator);

        //string testTypeName = "TestPostMto";
        string testTypeName = nameof(testPost);

        Assert.True(result.Count > 0);

        Assert.True(result.ContainsKey(testTypeName));

        Type testPostMtoType = result[testTypeName];

        //check select expression

        //System.Reflection.MethodInfo? method = testPostMtoType.GetMethod("selectExpression", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        var selectExpression = testPostMtoType.GetField("selectExpression", BindingFlags.Static | BindingFlags.Public).GetValue(null);

        //method.Invoke(testPostMtoType, new object[] { });

        var entityType = testPostMtoType;

        var query = ef.Posts.Where(s => s.Type == nameof(testPost));//.Select(selectExpression)

        MethodInfo method = typeof(Queryable)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
        //narrow the search before doing 'Single()'
              .First(mi => mi.Name == nameof(Queryable.Select)
                         // this check technically not required, but more future proof
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 2
                         && mi.GetParameters()[1].Name == "selector")
              .MakeGenericMethod(typeof(Post), entityType);

#pragma warning disable CS0219 // Variable is assigned but its value is never used
        Expression<Func<Post, Post>>? exaample = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used

        var fnt = typeof(Func<,>).MakeGenericType(typeof(Post), entityType);

        var query2 = method.Invoke(query, new object[] { query, selectExpression! }) as IQueryable<Post>;
        //var query2 = method.Invoke(query, new object[] { query, _delegate });

        var posts = query2!.ToList();

        Assert.True(posts.Any());


    }

    [Fact]
    void Sample()
    {
        using var ef = _serviceProvider.GetService<MarsDbContextLegacy>();

        var posts = ef.Posts
                        .Include(s => s.User)
                        .Include(s => s.MetaValues)
                            .ThenInclude(s => s.MetaField)
                        .AsNoTracking()
                        .Where(s => s.Type == nameof(testPost))
                        .Select(TestPostMto.selectExpression)
                        .ToList();

        Assert.True(posts.Any());
    }

    [Fact]
    void SampleByDeclaredExpression()
    {
        using var ef = _serviceProvider.GetService<MarsDbContextLegacy>();

        Type entityType = typeof(TestPostMto);

        var query = ef.Posts
                        .Include(s => s.User)
                        .Include(s => s.MetaValues)
                            .ThenInclude(s => s.MetaField)
                        .AsNoTracking()
                        .Where(s => s.Type == nameof(testPost));

        MethodInfo method = typeof(Queryable)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
              .First(mi => mi.Name == nameof(Queryable.Select)
                         // this check technically not required, but more future proof
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 2
                         && mi.GetParameters()[1].Name == "selector")
              .MakeGenericMethod(typeof(Post), entityType);

        IQueryable<TestPostMto> postsQuery;

        if (true)
        {
            var selectExpression = typeof(TestPostMto).GetField("selectExpression", BindingFlags.Static | BindingFlags.Public).GetValue(null);

            postsQuery = (method.Invoke(query, new object[] { query, selectExpression! }) as IQueryable<TestPostMto>)!;
        }
        else
        {
#pragma warning disable CS0162 // Unreachable code detected
            postsQuery = method.Invoke(query, new object[] { query, TestPostMto.selectExpression }) as IQueryable<TestPostMto>;
#pragma warning restore CS0162 // Unreachable code detected

        }

        var posts = postsQuery.OrderBy(s => s.int1).ToList();

        Assert.True(posts.Any());
    }

    //[Fact]
    //public void EXpr2()
    //{
    //    //Func<Post, TestPostMto> func = TestPostMto.selectExpression;
    //    var fnt = typeof(Func<,>).MakeGenericType(typeof(Post), typeof(TestPostMto));


    //    //LambdaExpression lambda = Expression.Lambda(
    //    //    //Expression.Constant(func)
    //    //    Expression.Call(null, TestPostMto.selectExpression.GetMethodInfo())
    //    //    //Expression.Parameter(typeof(Post), "parameter"),
    //    //    //Expression.Parameter(typeof(TestPostMto), "parameter")
    //    //);

    //    ////System.Linq.Expressions.Expression`1[System.Func`2[AppShared.Models.Post,Test.Mars.Host.WorkAboutSelectExpression+TestPostMto]]
    //    //Expression<Func<Post, TestPostMto>> fine = post => new TestPostMto();

    //    //Assert.Equal(fine.Type, lambda.Type);
    //}

    [Fact]
    public void TestUpdateMetaModelMtoRuntimeCompiledTypes()
    {
        IMetaModelTypesLocator mlocator = _serviceProvider.GetRequiredService<IMetaModelTypesLocator>();

        mlocator.UpdateMetaModelMtoRuntimeCompiledTypes(_serviceProvider);

        Assert.True(mlocator.MetaMtoModelsCompiledTypeDict.Count > 0);
    }

    //[Fact]
    //public void TestPostTypeChagedAndRequireRecompile()
    //{
    //    using var ef = _serviceProvider.GetService<MarsDbContext>();

    //    PostType postType = ef.PostTypes.Include(s => s.MetaFields).First(s => s.TypeName == nameof(testPost));


    //}
}

