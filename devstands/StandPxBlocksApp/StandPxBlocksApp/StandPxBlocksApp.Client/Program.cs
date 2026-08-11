using Blazored.LocalStorage;
using Flurl.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StandPxBlocksApp.Client.Startups;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var backendUrl = builder.HostEnvironment.BaseAddress.TrimEnd('/');

var httpClient = new HttpClient() { BaseAddress = new Uri(backendUrl) };
builder.Services.AddScoped(sp => httpClient);
builder.Services.AddScoped<IFlurlClient>(sp => new FlurlClient(httpClient));

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddLocalization();
builder.ConfigureAppLanguage();

var app = builder.Build();
await app.RunAsync();
