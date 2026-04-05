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
            return OperationResult.Success("Компания не определена. Company rules пропущены.");
        }

        var loadResult = await _ruleStore.LoadAsync();
        if (!loadResult.IsSuccess)
        {
            _currentCompanyId = companyId;
            _activeRules = [];
            return OperationResult.Failure(loadResult.Message, loadResult.ErrorCode);
        }

        var cache = loadResult.Value;
        _currentCompanyId = companyId;

        if (cache == null || cache.CompanyId != companyId)
        {
            _activeRules = [];
            return OperationResult.Success("Подходящий локальный кеш company rules не найден.");
        }

        _activeRules = OrderActiveRules(cache.Rules);
        return OperationResult.Success($"Загружено company rules из локального кеша: {_activeRules.Count}.");
    }

    public async Task<OperationResult<int>> RefreshRulesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            return OperationResult<int>.Success(0, "Компания не определена. Refresh company rules пропущен.");
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var apiResult = await _apiService.GetCompanyRulesAsync(companyId, cancellationToken);
            if (!apiResult.IsSuccess || apiResult.Value == null)
            {
                AppLogger.Log($"Company rules refresh skipped: {apiResult.Message}", "ERROR");
                return OperationResult<int>.Failure(apiResult.Message, apiResult.ErrorCode);
            }

            var cache = new ActivityClassificationRulesCache
            {
                CompanyId = companyId,
                RefreshedAt = DateTimeOffset.UtcNow,
                Rules = apiResult.Value.ToList()
            };

            var saveResult = await _ruleStore.SaveAsync(cache);
            if (!saveResult.IsSuccess)
            {
                AppLogger.Log(saveResult.Message, "ERROR");
                return OperationResult<int>.Failure(saveResult.Message, saveResult.ErrorCode);
            }

            _currentCompanyId = companyId;
            _activeRules = OrderActiveRules(cache.Rules);
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
            return null;
        }

        var normalizedAppName = Normalize(appName);
        if (string.IsNullOrWhiteSpace(normalizedAppName))
        {
            return null;
        }

        var normalizedWindowTitle = Normalize(windowTitle);

        foreach (var rule in _activeRules)
        {
            if (!MatchesPattern(normalizedAppName, rule.AppNamePattern))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(rule.WindowTitlePattern) &&
                !MatchesPattern(normalizedWindowTitle, rule.WindowTitlePattern))
            {
                continue;
            }

            return new ActivityClassificationDecision
            {
                Category = rule.Category,
                Source = ActivityClassificationSource.CompanyRule,
                MatchedRuleId = rule.Id,
                MatchedRuleDescription = BuildRuleDescription(rule)
            };
        }

        return null;
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
