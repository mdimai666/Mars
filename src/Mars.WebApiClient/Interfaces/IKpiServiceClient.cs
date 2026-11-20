using Mars.Contracts.Common;

namespace Mars.WebApiClient.Interfaces;

public interface IKpiServiceClient
{
    Task<IReadOnlyDictionary<string, KpiResult>> Get(IEnumerable<string> keys);
}
