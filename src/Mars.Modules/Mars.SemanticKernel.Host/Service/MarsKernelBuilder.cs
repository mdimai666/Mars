using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Mars.SemanticKernel.Host.Service;

// Not used
internal sealed class MarsKernelBuilder : IKernelBuilder, IKernelBuilderPlugins
{
    private IServiceCollection? _services;

    public MarsKernelBuilder()
    {
        this.AllowBuild = true;
    }

    public MarsKernelBuilder(IServiceCollection services)
    {
        this._services = services;
    }

    /// <summary>Whether to allow a call to Build.</summary>
    /// <remarks>As a minor aid to help avoid misuse, we try to prevent Build from being called on instances returned from AddKernel.</remarks>
    internal bool AllowBuild { get; }

    /// <summary>Gets the collection of services to be built into the <see cref="Kernel"/>.</summary>
    public IServiceCollection Services => this._services ??= new ServiceCollection();

    /// <summary>Gets a builder for plugins to be built as services into the <see cref="Kernel"/>.</summary>
    public IKernelBuilderPlugins Plugins => this;
}
