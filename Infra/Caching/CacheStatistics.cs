namespace Project.Infra.Caching;

/// <summary>
/// Telemetry record holding cache diagnostic metrics, file counts, and disk usage.
/// </summary>
public record CacheStatistics(
    int TotalCachedFiles,
    long TotalSizeBytes,
    int CacheHits,
    int CacheMisses,
    string CacheDirectory
)
{
    public int TotalRequests => CacheHits + CacheMisses;
    public double HitRatio => TotalRequests == 0 ? 0.0 : (double)CacheHits / TotalRequests;
}
