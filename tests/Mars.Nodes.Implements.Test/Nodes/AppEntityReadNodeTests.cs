using FluentAssertions;
using Mars.Host.Data.Common;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Nodes.Implements.Test.Services;
using Mars.QueryLang.Host.Helpers;
using Mars.QueryLang.Host.Services;
using Mars.WebApp.Nodes.Host.Nodes;
using Mars.WebApp.Nodes.Models.NodeEntityQuery;

namespace Mars.Nodes.Implements.Test.Nodes;

public class AppEntityReadNodeTests : NodeServiceUnitTestBase
{
    [Fact]
    public void BuildString__ShouldOk()
    {
        //Arrange
        var qs = new NodeEntityQueryRequestModel
        {
            EntityName = "post", // Post.post
            CallChains = [
                new() { MethodName = "Where", Parameters = ["post.Title!=\"111\""] },
                new() { MethodName = "ToList", },
            ]
        };
        //Act
        var expression = qs.BuildString();

        //Assert
        expression.Should().Be("post.Where(post.Title!=\"111\").ToList()");

        //That like that

        var qef = new EfStringQuery<PostEntity>(default!, default!);

        var posts = () => qef.Where(s => s.Title != "111").ToList();//subset of PostEntity with PostTypeNae == "post"
    }

    // For experiments
    //private void ParseExpression__ShouldOk()
    //{
    //    //Arrange
    //    _ = nameof(AppEntityReadNodeImpl.Execute);
    //    _ = nameof(EfStringQuery<IBasicEntity>);
    //    var ef = new MarsDbContext(default!);
    //    var qef = new EfStringQuery<PostEntity>(ef.Posts, new());

    //    var posts = qef.Where(s => s.Title != "111").ToList();

    //    var helper = new QueryLangHelperAvailableMethodsProvider();

    //    var methods = helper.AvailableMethods();
    //}
}
