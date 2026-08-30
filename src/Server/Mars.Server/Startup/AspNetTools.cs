using System.IO.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Server.Startup;

public static class AspNetTools
{
    public static IServiceCollection AddAspNetTools(this IServiceCollection services)
    {
        services.AddResponseCaching()
                .AddMemoryCache(options =>
                {
                    options.TrackStatistics = true;
                })
                .AddLogging();

        services.AddResponseCompression(opts =>
        {
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
            opts.EnableForHttps = true;
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
        })
            .Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal)
            .Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // if don't set default value is: 30 MB
        });

        services.Configure<FormOptions>(x =>
        {
            x.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024;// 2GB
        });

        return services;
    }
}
