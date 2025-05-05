#Mars plugin

##usage
```csharp
//CreateBuilder
builder.AddPlugins();
builder.Services.AddControllers().AddPluginsAsPartOfMvc();

//Configure
app.UsePlugins();
```

## exaple

```csharp
using Mars.Plugin;
using Shared;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllers().PartManager;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.


builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.AddScoped<PostService>();

builder.AddPlugins();
builder.Services.AddControllers().AddPluginsAsPartOfMvc();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = "api";
});
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UsePlugins();
app.MapControllers();
app.MapRazorPages();


app.Run();


```