using Mars.HttpSmartAuthFlow.Strategies;

namespace Mars.HttpSmartAuthFlow;

public interface IAuthStrategyFactory
{
    IAuthStrategy Create(AuthConfig config);
}
