using Mars.Host.Data;
using Mars.Host.QueryLang;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Microsoft.Extensions.DependencyInjection;
using Test.Mars.Host.Models;
using Test.Mars.Host.TestHostApp;
using static Test.Mars.Host.SomeTests.WorkAboutSelectExpression;

namespace Test.Mars.Host.QueryLang;

public class TestQueryLangFunctionality : UnitTestHostBaseClass
{

    [Fact]
    public void TestSelect()
    {
        //var pctx = _pctx();
        var renderContext = _renderContext();
        XInterpreter ppt = new(renderContext, null);
        var ef = _serviceProvider.GetService<MarsDbContextLegacy>();


        EntityQuery eq = new EntityQuery(_serviceProvider, ppt, null);


        string query1 = "testPost.OrderBy(Id).Select(Id)";

        IEnumerable<Guid> postsActualIds = (eq.Query(query1) as IEnumerable<Guid>)!.ToList();

        IEnumerable<Guid> postsIdsExpected = ef.Posts.Where(s => s.Type == nameof(testPost)).OrderBy(s => s.Id).Select(s => s.Id).ToList();

        Assert.Equivalent(postsIdsExpected, postsActualIds);
    }

    [Fact]
    public void TestSelectAsType()
    {
        //var pctx = _pctx();
        var renderContext = _renderContext();
        XInterpreter ppt = new(renderContext, null);
        var ef = _serviceProvider.GetService<MarsDbContextLegacy>();

        string postTypeName = TestPostMto._TypeName;

        IMetaModelTypesLocator mlocator = _serviceProvider.GetRequiredService<IMetaModelTypesLocator>();

        mlocator.TryUpdateMetaModelMtoRuntimeCompiledTypes(_serviceProvider);
        Type mtoType = mlocator.MetaMtoModelsCompiledTypeDict[postTypeName];

        EntityQuery eq = new EntityQuery(_serviceProvider, ppt, null);
        string query1 = $"testPost.OrderBy(Id).Select({nameof(TestPostMto.int1)})";

        //its different types: TestPostMto and mtoType(runtime compiled)

        IEnumerable<int> postsActualIds = (eq.Query(query1) as IEnumerable<int>)!;

        IEnumerable<int> postsIdsExpected = ef.Posts.Where(s => s.Type == TestPostMto._TypeName).OrderBy(s => s.Id).Select(TestPostMto.selectExpression).Select(s => s.int1).ToList();

        Assert.Equivalent(postsIdsExpected, postsActualIds);
    }
}
