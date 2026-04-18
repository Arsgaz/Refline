using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refline.Business.Identity;
using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Services.ActivityClassification;

public sealed class ActivityClassificationRulesApiService : IActivityClassificationRulesApiService
{
    private readonly HttpClient _httpClient;
    private readonly ApiAuthorizationService _apiAuthorizationService;
    private readonly ICurrentUserSessionStore _currentUserSessionStore;
    private readonly JsonSerializerOptions _jsonOptions;

    public ActivityClassificationRulesApiService(
        HttpClient httpClient,
        ApiAuthorizationService apiAuthorizationService,
        ICurrentUserSessionStore currentUserSessionStore)
    {
        _httpClient = httpClient;
        _apiAuthorizationService = apiAuthorizationService;
        _currentUserSessionStore = currentUserSessionStore;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<OperationResult<IReadOnlyList<ActivityClassificationRule>>> GetMyCompanyRulesAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserSessionStore.GetCurrentUser();
        if (currentUser == null)
        {
            return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Failure(
                "Пользовательская сессия не найдена для загрузки classification rules.",
                "CLASSIFICATION_RULES_SESSION_MISSING");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/classification-rules/me");
            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(request, cancellationToken);
            if (!authorizeResult.IsSuccess)
            {
                return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Failure(
                    authorizeResult.Message,
                    authorizeResult.ErrorCode);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Failure(
                    errorMessage,
                    $"HTTP_{(int)response.StatusCode}");
            }

            var rules = await response.Content.ReadFromJsonAsync<List<EmployeeClassificationRuleDto>>(_jsonOptions, cancellationToken);
            return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Success(
                (rules ?? []).Select(MapRule).ToList());
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Failure(
                $"API недоступен: {ex.Message}",
                "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Failure(
                $"Превышено время ожидания API: {ex.Message}",
                "API_TIMEOUT");
        }
    }

    private async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(_jsonOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch (JsonException)
        {
        }

        var fallback = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(fallback)
            ? $"Ошибка API: HTTP {(int)response.StatusCode}."
            : fallback;
    }

    private static ActivityClassificationRule MapRule(EmployeeClassificationRuleDto rule)
    {
        return new ActivityClassificationRule
        {
            Id = rule.Id,
            AppNamePattern = rule.AppNamePattern ?? string.Empty,
            WindowTitlePattern = string.IsNullOrWhiteSpace(rule.WindowTitlePattern)
                ? null
                : rule.WindowTitlePattern,
            Category = rule.Category,
            Priority = rule.Priority,
            IsEnabled = true
        };
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }

    private sealed class EmployeeClassificationRuleDto
    {
        public long Id { get; set; }

        public string? AppNamePattern { get; set; }

        public string? WindowTitlePattern { get; set; }

        public ActivityCategory Category { get; set; }

        public int Priority { get; set; }
    }
}
