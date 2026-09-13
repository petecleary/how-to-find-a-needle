namespace PI.SearchApi.Data;

public interface IDatabaseManager
{
    Task InitDbAsync(CancellationToken cancellationToken = default);
}
