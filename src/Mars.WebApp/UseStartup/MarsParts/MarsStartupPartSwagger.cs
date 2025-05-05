using System.Diagnostics;
using System.Reflection;
using Mars.Host.Features;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartSwagger
{
    public static IServiceCollection MarsAddSwagger(this IServiceCollection services)
    {
        //TODO: replace with https://devblogs.microsoft.com/dotnet/dotnet9-openapi/

        // Register the Swagger generator, defining 1 or more Swagger documents
        services
            .AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "API",
                    Version = "v1",
                });

                c.UseDateOnlyTimeOnlyStringConverters();
                c.SchemaFilter<DescribeEnumMemberValues>();
                c.DocumentFilter<FeatureGateSwaggerFilter>();
                c.OperationFilter<FeatureGateOperationFilter>();
                //c.UseInlineDefinitionsForEnums();

                //Allow Auth
                var jwtSecurityScheme = new OpenApiSecurityScheme
                {
                    BearerFormat = "JWT",
                    Name = "JWT Authentication",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };

                var apiKeySecurityScheme = new OpenApiSecurityScheme
                {
                    Name = "X-API-Key",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Description = "Put Api key",

                    Reference = new OpenApiReference
                    {
                        Id = "ApiKey",
                        Type = ReferenceType.SecurityScheme
                    }
                };

                c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
                c.AddSecurityDefinition(apiKeySecurityScheme.Reference.Id, apiKeySecurityScheme);

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtSecurityScheme, Array.Empty<string>() },
                    { apiKeySecurityScheme, Array.Empty<string>() }
                });
                //end allow auth

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

        return services;
    }

    public static IApplicationBuilder MarsUseSwagger(this IApplicationBuilder app)
    {
        IOptionService optionService = app.ApplicationServices.GetRequiredService<IOptionService>()!;


        app.Use([DebuggerStepThrough] async (context, next) =>
        {
            if (context.Request.Method == "GET" &&
                (context.Request.Path == "/swagger/v1/swagger.json"
                || context.Request.Path == "/api/index.html"))
            {

                var apiOption = optionService.GetOption<ApiOption>();

                if (apiOption.ViewMode == ApiOption.EViewMode.None)
                {
                    context.Response.StatusCode = 404;
                    return;
                }
                else if (apiOption.ViewMode == ApiOption.EViewMode.Auth)
                {
                    if (context.User.Identity.IsAuthenticated == false)
                    {
                        context.Response.StatusCode = 404;
                        return;
                    }
                }

            }

            await next.Invoke();
        });

        // Enable middleware to serve generated Swagger as a JSON endpoint.
        app.UseSwagger();
        // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
        // specifying the Swagger JSON endpoint.
        //https://docs.microsoft.com/ru-ru/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-3.1&tabs=visual-studio#add-and-configure-swagger-middleware
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");

            c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);

            //set swagger page as index
            //c.RoutePrefix = string.Empty;
            c.RoutePrefix = "api"; // TODO: add api/v1
        });

        return app;
    }

    public static IEndpointRouteBuilder MarsUseEndpointApiFallback(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFallback("/api/{**slug}", ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            ctx.Response.WriteAsJsonAsync(new { Ok = false, Message = "ApiNotFound" });
            return Task.CompletedTask;
        });

        return endpoints;
    }

}
