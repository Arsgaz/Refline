using Refline.Data.Activity;
using Refline.Data.Infrastructure;
using Refline.Models;
using Refline.Services.ActivityClassification;
using Refline.Utils;

namespace Refline.Business.Activity;

public sealed class CompanyActivityClassificationService : ICompanyActivityClassificationService
{
    private readonly IActivityClassificationRuleStore _ruleStore;
    private readonly IActivityClassificationRulesApiService _apiService;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private Guid? _currentCompanyId;
    private IReadOnlyList<ActivityClassificationRule> _activeRules = [];
    private int _lastApiLoadedRulesCount;
    private int _lastCachedRulesCount;
    private int _lastSavedRulesCount;
    private string _lastApiStatus = "API rules ещё не загружались.";
    private string _lastCacheStatus = "Локальный кеш rules ещё не читался.";
    private string _lastRefreshStatus = "Refresh rules ещё не выполнялся.";
    private string _lastMatchStatus = "Сопоставление rules ещё не выполнялось.";
    private string _lastMatchTrace = "Нет данных.";

    public CompanyActivityClassificationService(
        IActivityClassificationRuleStore ruleStore,
        IActivityClassificationRulesApiService apiService)
    {
        _ruleStore = ruleStore;
        _apiService = apiService;
    }

    public async Task<OperationResult> RestoreCachedRulesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (companyId == Guid.Empty)
        {
            _currentCompanyId = null;
            _activeRules = [];
            _lastCacheStatus = "Компания не определена. Локальный кеш rules пропущен.";
            return OperationResult.Success("Компания не определена. Company rules пропущены.");
        }

        var loadResult = await _ruleStore.LoadAsync();
        if (!loadResult.IsSuccess)
        {
            _currentCompanyId = companyId;
            _activeRules = [];
            _lastCachedRulesCount = 0;
            _lastCacheStatus = loadResult.Message;
            return OperationResult.Failure(loadResult.Message, loadResult.ErrorCode);
        }

        var cache = loadResult.Value;
        _currentCompanyId = companyId;

        if (cache == null || cache.CompanyId != companyId)
        {
            _activeRules = [];
            _lastCachedRulesCount = cache?.Rules.Count ?? 0;
            _lastCacheStatus = cache == null
                ? "Локальный кеш company rules отсутствует."
                : "Локальный кеш rules найден, но относится к другой компании.";
            return OperationResult.Success("Подходящий локальный кеш company rules не найден.");
        }

        _activeRules = OrderActiveRules(cache.Rules);
        _lastCachedRulesCount = cache.Rules.Count;
        _lastCacheStatus = $"Из локального кеша прочитано {cache.Rules.Count} rule(s), active after filter: {_activeRules.Count}.";
        return OperationResult.Success($"Загружено company rules из локального кеша: {_activeRules.Count}.");
    }

    public async Task<OperationResult<int>> RefreshRulesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            _lastRefreshStatus = "Компания не определена. Refresh rules пропущен.";
            return OperationResult<int>.Success(0, "Компания не определена. Refresh company rules пропущен.");
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var apiResult = await _apiService.GetMyCompanyRulesAsync(cancellationToken);
            if (!apiResult.IsSuccess || apiResult.Value == null)
            {
                _lastApiLoadedRulesCount = 0;
                _lastApiStatus = apiResult.Message;
                _lastRefreshStatus = $"Refresh rules failed: {apiResult.Message}";
                AppLogger.Log($"Company rules refresh skipped: {apiResult.Message}", "ERROR");
                return OperationResult<int>.Failure(apiResult.Message, apiResult.ErrorCode);
            }

            _lastApiLoadedRulesCount = apiResult.Value.Count;
            _lastApiStatus = $"API returned {_lastApiLoadedRulesCount} rule(s).";

            var cache = new ActivityClassificationRulesCache
            {
                CompanyId = companyId,
                RefreshedAt = DateTimeOffset.UtcNow,
                Rules = apiResult.Value.ToList()
            };

            var saveResult = await _ruleStore.SaveAsync(cache);
            if (!saveResult.IsSuccess)
            {
                _lastSavedRulesCount = 0;
                _lastRefreshStatus = $"Rules loaded from API, but cache save failed: {saveResult.Message}";
                AppLogger.Log(saveResult.Message, "ERROR");
                return OperationResult<int>.Failure(saveResult.Message, saveResult.ErrorCode);
            }

            _currentCompanyId = companyId;
            _activeRules = OrderActiveRules(cache.Rules);
            _lastSavedRulesCount = cache.Rules.Count;
            _lastCachedRulesCount = cache.Rules.Count;
            _lastCacheStatus = $"В локальный кеш сохранено {cache.Rules.Count} rule(s).";
            _lastRefreshStatus = $"Refresh rules succeeded: API {_lastApiLoadedRulesCount}, saved {_lastSavedRulesCount}, active {_activeRules.Count}.";
            AppLogger.Log($"Company rules refreshed: {_activeRules.Count} active rule(s) loaded.");
            return OperationResult<int>.Success(_activeRules.Count, $"Загружено active company rules: {_activeRules.Count}.");
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<OperationResult> ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var clearResult = await _ruleStore.ClearAsync();
        if (!clearResult.IsSuccess)
        {
            return OperationResult.Failure(clearResult.Message, clearResult.ErrorCode);
        }

        _currentCompanyId = null;
        _activeRules = [];
        _lastCachedRulesCount = 0;
        _lastSavedRulesCount = 0;
        _lastCacheStatus = "Локальный кеш rules очищен.";
        return OperationResult.Success();
    }

    public ActivityCategory? TryClassify(string appName, string? windowTitle)
    {
        return TryClassifyDetailed(appName, windowTitle)?.Category;
    }

    public ActivityClassificationDecision? TryClassifyDetailed(string appName, string? windowTitle)
    {
        if (_currentCompanyId == null || _activeRules.Count == 0)
        {
            _lastMatchStatus = "Company rules не применялись: active rules = 0.";
            _lastMatchTrace = $"Input: App='{appName}', Window='{windowTitle}'. Active rules count: 0.";
            return null;
        }

        var normalizedAppName = Normalize(appName);
        if (string.IsNullOrWhiteSpace(normalizedAppName))
        {
            _lastMatchStatus = "Company rules не применялись: AppName пустой.";
            _lastMatchTrace = $"Input: App='{appName}', Window='{windowTitle}'.";
            return null;
        }

        var normalizedWindowTitle = Normalize(windowTitle);
        var traceLines = new List<string>
        {
            $"Input App='{normalizedAppName}', Window='{normalizedWindowTitle}', Active rules={_activeRules.Count}."
        };

        foreach (var rule in _activeRules)
        {
            var normalizedRuleAppPattern = Normalize(rule.AppNamePattern);
            var normalizedRuleWindowPattern = Normalize(rule.WindowTitlePattern);

            if (!MatchesPattern(normalizedAppName, normalizedRuleAppPattern))
            {
                traceLines.Add($"Rule #{rule.Id} skipped: app mismatch. AppPattern='{normalizedRuleAppPattern}'.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(normalizedRuleWindowPattern) &&
                !MatchesPattern(normalizedWindowTitle, normalizedRuleWindowPattern))
            {
                traceLines.Add($"Rule #{rule.Id} skipped: window mismatch. WindowPattern='{normalizedRuleWindowPattern}'.");
                continue;
            }

            var matchDescription = BuildRuleDescription(rule);
            traceLines.Add($"Rule #{rule.Id} matched.");
            _lastMatchStatus = $"Company rule matched: {matchDescription}";
            _lastMatchTrace = string.Join(Environment.NewLine, traceLines);

            return new ActivityClassificationDecision
            {
                Category = rule.Category,
                Source = ActivityClassificationSource.CompanyRule,
                MatchedRuleId = rule.Id,
                MatchedRuleDescription = matchDescription
            };
        }

        _lastMatchStatus = "Company rules checked, but no rule matched.";
        _lastMatchTrace = string.Join(Environment.NewLine, traceLines);
        return null;
    }

    public ActivityClassificationDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        return new ActivityClassificationDiagnosticsSnapshot
        {
            ApiLoadedRulesCount = _lastApiLoadedRulesCount,
            CachedRulesCount = _lastCachedRulesCount,
            SavedRulesCount = _lastSavedRulesCount,
            ActiveRulesCount = _activeRules.Count,
            LastApiStatus = _lastApiStatus,
            LastCacheStatus = _lastCacheStatus,
            LastRefreshStatus = _lastRefreshStatus,
            LastMatchStatus = _lastMatchStatus,
            LastMatchTrace = _lastMatchTrace
        };
    }

    private static IReadOnlyList<ActivityClassificationRule> OrderActiveRules(IEnumerable<ActivityClassificationRule>? rules)
    {
        return (rules ?? [])
            .Where(rule => rule.IsEnabled && !string.IsNullOrWhiteSpace(rule.AppNamePattern))
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Id)
            .ToList();
    }

    private static bool MatchesPattern(string source, string? pattern)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        return source.Contains(pattern.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string BuildRuleDescription(ActivityClassificationRule rule)
    {
        return string.IsNullOrWhiteSpace(rule.WindowTitlePattern)
            ? $"Rule #{rule.Id}: App contains \"{rule.AppNamePattern}\""
            : $"Rule #{rule.Id}: App contains \"{rule.AppNamePattern}\", Window contains \"{rule.WindowTitlePattern}\"";
    }
}
