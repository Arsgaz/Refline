namespace Refline.Models;

public sealed class ActivityClassificationDiagnosticsSnapshot
{
    public int ApiLoadedRulesCount { get; init; }

    public int CachedRulesCount { get; init; }

    public int SavedRulesCount { get; init; }

    public int ActiveRulesCount { get; init; }

    public string LastApiStatus { get; init; } = "API rules ещё не загружались.";

    public string LastCacheStatus { get; init; } = "Локальный кеш rules ещё не читался.";

    public string LastRefreshStatus { get; init; } = "Refresh rules ещё не выполнялся.";

    public string LastMatchStatus { get; init; } = "Сопоставление rules ещё не выполнялось.";

    public string LastMatchTrace { get; init; } = "Нет данных.";
}
