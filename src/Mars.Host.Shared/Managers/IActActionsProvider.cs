using System.Reflection;

namespace Mars.Host.Shared.Managers;

public interface IActActionsProvider
{
    void RegisterAssembly(Assembly assembly);
}
