namespace Mars.Contracts.Common;

public interface IKpiHandler
{
    string Key { get; }
    Task<KpiResult> GetAsync(CancellationToken cancellationToken);
}
