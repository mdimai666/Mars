using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();


app.MapRazorPages();
app.MapControllers();

app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/dev"), first =>
{
    var options = new RewriteOptions()
                //.AddRedirect("(.*)/$", "$1")                // удаление концевого слеша
                //.AddRedirect("(?i)home[/]?$", "home/index"); // переадресация с home на home/index
                //.AddRewrite("^dev/(?!_content/)(.*)", "dev/$1", false);
                //.AddRewrite("^dev/monaco(.*)", "dev/monaco$1", true)
                .AddRewrite("^dev/_content/(.*)", "_content/$1", false);

    first.UseRewriter(options);

    first.UseBlazorFrameworkFiles("/dev");
    first.UseStaticFiles();
    first.UseRouting();

    first.UseAuthorization();


    first.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();

        ////////IList<IRouter> aa;
        ////////aa.

        //endpoints.



        //endpoints.MapGet("/dd", async context =>
        //{
        //    await context.Response.WriteAsJsonAsync("dd");
        //});

        //endpoints.MapFallbackToFile("AppAdmin/{*path:nonfile}", "AppAdmin/index.html");
        //endpoints.MapFallbackToPage("/_AdminHost");
        endpoints.MapFallbackToFile("/dev/index.html");


        //endpoints.MapFallback(async (req) => {
        //    await req.Response.WriteAsync("Ok");
        //});
    });
});

app.Run();
