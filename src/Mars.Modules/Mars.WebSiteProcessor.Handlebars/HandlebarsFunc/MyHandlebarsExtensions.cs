using HandlebarsDotNet.PathStructure;
using HandlebarsDotNet.ValueProviders;

namespace Mars.Host.Templators.HandlebarsFunc;

public static class MyHandlebarsExtensions
{
    public static HandlebarsHelperFunctionContext RenderContext(this DataValues data)
    {
        var renderContext = data[ChainSegment.Create(HandlebarsHelperFunctionContext.HelperFunctionContextKey)] as HandlebarsHelperFunctionContext;
        if (renderContext is null)
        {
            throw new Exception("HandlebarsHelperFunctionContext not found");
        }
        return renderContext;
    }
}
