using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;
using Mars.Host.Features;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Mars.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

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
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "API",
                    Version = "v1",
                });

                c.SchemaFilter<DescribeEnumMemberValues>();
                c.DocumentFilter<FeatureGateSwaggerFilter>();
                c.OperationFilter<FeatureGateOperationFilter>();
                c.UseCustomizeMetaDictionarySchemaFilter();
                c.UseCustomizeSourceUriTypeSchemaFilter();
                //c.UseInlineDefinitionsForEnums();

                //Allow Auth
                var jwtSecurityScheme = new OpenApiSecurityScheme
                {
                    BearerFormat = "JWT",
                    Name = "JWT Authentication",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    Description = "Put **_ONLY_** your JWT Bearer token on textbox below!"
                };

                var apiKeySecurityScheme = new OpenApiSecurityScheme
                {
                    Name = "X-API-Key",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Description = "Put Api key"
                };

                c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtSecurityScheme);
                c.AddSecurityDefinition("ApiKey", apiKeySecurityScheme);

                c.AddSecurityRequirement(openApiDocument => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, openApiDocument)] = [],
                    [new OpenApiSecuritySchemeReference("ApiKey", openApiDocument)] = [],
                });
                //end allow auth

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                c.TagActionsBy(ControllersGroupingByName);
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
        app.UseSwagger(options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
        });
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

    // Добавляет возможно группировки по Name="group1", основанным на RouteAttribute контроллера
    static IList<string> ControllersGroupingByName(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription apiDesc)
    {
        if (apiDesc.ActionDescriptor is ControllerActionDescriptor cad)
        {
            // Берём RouteAttribute только с контроллера
            var ctrlRouteName = cad.ControllerTypeInfo
                .GetCustomAttributes(typeof(RouteAttribute), true)
                .Cast<RouteAttribute>()
                .FirstOrDefault()?.Name;

            if (!string.IsNullOrEmpty(ctrlRouteName))
                return [ctrlRouteName!];
        }

        // Фоллбэк по имени контроллера
        return [apiDesc.ActionDescriptor.RouteValues["controller"] ?? "Misc"];
    }

    static void UseCustomizeMetaDictionarySchemaFilter(this SwaggerGenOptions options)
    {
        options.MapType<IReadOnlyDictionary<string, object?>>(() => new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            AdditionalProperties = new OpenApiSchema
            {
                OneOf =
            [
                new OpenApiSchema { Type = JsonSchemaType.String },
                new OpenApiSchema { Type = JsonSchemaType.Number },
                new OpenApiSchema { Type = JsonSchemaType.Integer },
                new OpenApiSchema { Type = JsonSchemaType.Boolean },
                new OpenApiSchema { Type = JsonSchemaType.Object },
                new OpenApiSchema { Type = JsonSchemaType.Array }
            ]
            },
            Example = new JsonObject
            {
                ["key1"] = 123,
                ["key2"] = "value",
                ["key3"] = true
            }
        });

        options.OperationFilter<PostJsonExamplesFilter>();

    }

    static void UseCustomizeSourceUriTypeSchemaFilter(this SwaggerGenOptions options)
    {
        options.MapType<SourceUri>(() => new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Format = null,

            Example = "/Post/post",

            Properties = null,
            AdditionalPropertiesAllowed = false,
        });

    }
}
