using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Data.Contexts;

public class MarsDbContextOptionExtension : IDbContextOptionsExtension
{
    DbContextOptionsExtensionInfo _info;
    public DbContextOptionsExtensionInfo Info => _info;

    public IMarsDbContextFactory Factory { get; }

    public MarsDbContextOptionExtension(IMarsDbContextFactory factory)
    {
        Factory = factory;
        _info = new MarsDbExtensionInfo(this);
    }

    public void ApplyServices(IServiceCollection services)
    {

    }

    public void Validate(IDbContextOptions options)
    {

    }

    private class MarsDbExtensionInfo : DbContextOptionsExtensionInfo
    {
        public MarsDbExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
        {
        }

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            //debugInfo["MyCustomField"] = ((MyDbContextOptionsExtension)Extension).MyCustomField ?? "<null>";
        }

        public override string LogFragment => $"Factory={((MarsDbContextOptionExtension)Extension).Factory}";

        public override bool IsDatabaseProvider { get; } = false;
    }
}
