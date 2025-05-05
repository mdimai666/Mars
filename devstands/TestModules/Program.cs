using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;
using TestModules;

Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ru-RU");
Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentCulture = new System.Globalization.CultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentUICulture = new System.Globalization.CultureInfo("ru-RU");


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddLocalization();

builder.Services.AddFluentUIComponents();



//await builder.SetDefaultCulture();



await builder.Build().RunAsync();


//public static class WebAssemblyHostExtension
//{
//    public async static Task SetDefaultCulture(this WebAssemblyHostBuilder host)
//    {
//        var jsInterop = host.Services.GetRequiredService<IJSRuntime>();
//        var result = await jsInterop.InvokeAsync<string>("blazorCulture.get");
//        CultureInfo culture;
//        //if (result != null)
//        //    culture = new CultureInfo(result);
//        //else
//            culture = new CultureInfo("ru-RU");
//        CultureInfo.DefaultThreadCurrentCulture = culture;
//        CultureInfo.DefaultThreadCurrentUICulture = culture;
//    }
//}
