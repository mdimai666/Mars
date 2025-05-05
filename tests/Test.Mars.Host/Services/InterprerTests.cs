using Mars.Host.Shared.Templators;

namespace Test.Mars.Host.Services;

public class InterprerTests
{

    [Fact]
    public void Eval_SimpleExpression_ShouldSuccess()
    {
        XInterpreter ppt = new();

        var result = ppt.Get.Eval<int>("1+1");

        Assert.Equal(2, result);
    }

    [Fact]
    public void Eval_UseVariable_ShouldSuccess()
    {
        XInterpreter ppt = new();

        var result = ppt.Get.Eval<int>("1+x", new DynamicExpresso.Parameter("x", 2));

        Assert.Equal(3, result);
    }

}
