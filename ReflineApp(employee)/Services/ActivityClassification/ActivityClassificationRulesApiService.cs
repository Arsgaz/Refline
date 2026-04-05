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
    private const string RequestingUserIdHeader = "X-Refline-User-Id";

    private readonly HttpClient _httpClient;
    private readonly ICurrentUserSessionStore _currentUserSessionStore;
    private readonly JsonSerializerOptions _jsonOptions;

    public ActivityClassificationRulesApiService(HttpClient httpClient, ICurrentUserSessionStore currentUserSessionStore)
    {
        _httpClient = httpClient;
        _currentUserSessionStore = currentUserSessionStore;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<OperationResult<IReadOnlyList<ActivityClassificationRule>>> GetCompanyRulesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserSessionStore.GetCurrentUser();
        if (currentUser == null)
        {
            return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Failure(
                "Пользовательская сессия не найдена для загрузки classification rules.",
                "CLASSIFICATION_RULES_SESSION_MISSING");
        }

        var serverCompanyId = ApiIdentityIdMapper.ToServerId(companyId);
        var serverUserId = ApiIdentityIdMapper.ToServerId(currentUser.Id);
        if (serverCompanyId <= 0 || serverUserId <= 0)
        {
            return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Failure(
                "Не удалось определить серверные идентификаторы для загрузки classification rules.",
                "CLASSIFICATION_RULES_INVALID_SERVER_IDS");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/admin/companies/{serverCompanyId}/classification-rules");
            request.Headers.Add(RequestingUserIdHeader, serverUserId.ToString());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden &&
                    currentUser.Role == UserRole.Employee)
                {
                    errorMessage =
                        $"Employee-клиент не может загрузить company rules: endpoint '{request.RequestUri?.PathAndQuery}' " +
                        "требует admin access и возвращает 403. Нужен отдельный employee-readable endpoint или предварительная доставка кеша.";
                }

                return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Failure(
                    errorMessage,
                    $"HTTP_{(int)response.StatusCode}");
            }

            var rules = await response.Content.ReadFromJsonAsync<List<ActivityClassificationRule>>(_jsonOptions, cancellationToken);
            return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Success(rules ?? []);
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

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}
