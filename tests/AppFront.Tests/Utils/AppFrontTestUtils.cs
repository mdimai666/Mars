using AngleSharp.Html;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;

namespace AppFront.Tests.Utils;

public static class AppFrontTestUtils
{
    public static async Task<string> RenderComponent<TComponent>(Dictionary<string, object?> parametersDict)
        where TComponent : ComponentBase
    {
        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        var jsMock = Substitute.For<IJSRuntime>();
        services.AddSingleton<IJSRuntime>(jsMock);
        var flurl = Substitute.For<IFlurlClient>();
        services.AddSingleton(flurl);

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();


        await using var htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);

        var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            //var dictionary = new Dictionary<string, object?>
            //{
            //    { "Message", "Hello from the Render Message component!" }
            //};
            //var parameters = ParameterView.FromDictionary(dictionary);
            var parameters = ParameterView.FromDictionary(parametersDict);
            //var output = await htmlRenderer.RenderComponentAsync<DocViewer<Type>>(parameters);
            var output = await htmlRenderer.RenderComponentAsync<TComponent>(parameters);

            return output.ToHtmlString();
        });

        //Console.WriteLine(html);

        return PrettyHtml(html);
    }

    public static string PrettyHtml(string html)
    {
        // pretty Html
        var parser = new HtmlParser();
        var dom = parser.ParseDocument("<html><body></body></html>");
        var document = parser.ParseFragment(html, dom.Body!);

        using var writer = new StringWriter();
        document.ToHtml(writer, new PrettyMarkupFormatter
        {
            Indentation = "\t",
            NewLine = "\n",
        });
        var indentedText = writer.ToString();

        return indentedText;
    }
}
