using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Mars.Test.Common;

public sealed class TestHostEnvironment : IHostEnvironment
{
    public string ApplicationName { get; set; } = "ApplicationName";
    public IFileProvider ContentRootFileProvider { get; set; } = default!;
    public string ContentRootPath { get; set; } = "wwwRoot";
    public string EnvironmentName { get; set; } = "Development";
}
