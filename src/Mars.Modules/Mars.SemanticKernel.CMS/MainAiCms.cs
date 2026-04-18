using Mars.SemanticKernel.CMS.Agents;
using Mars.SemanticKernel.CMS.Posts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SemanticKernel.CMS;

public static class MainAiCms
{
    public static WebApplicationBuilder AddAiCmsHost(this WebApplicationBuilder builder)
    {
        CmsAgentHandler.RegisterCmsAgent(builder.Services, builder.Environment);

        builder.Services.AddTransient<IAiCreatePostHandler, AiCreatePostHandler>();

        return builder;
    }

    public static IApplicationBuilder UseAiCmsHost(this IApplicationBuilder app)
    {
        return app;
    }
}
