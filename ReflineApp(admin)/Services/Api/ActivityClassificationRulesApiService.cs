using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refline.Admin.Business.Identity;
using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Services.Api;

public sealed class ActivityClassificationRulesApiService : IActivityClassificationRulesService
{
    private readonly HttpClient _httpClient;
    private readonly CurrentSessionContext _currentSessionContext;
    private readonly JsonSerializerOptions _jsonOptions;

    public ActivityClassificationRulesApiService(HttpClient httpClient, CurrentSessionContext currentSessionContext)
    {
        _httpClient = httpClient;
        _currentSessionContext = currentSessionContext;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<OperationResult<IReadOnlyList<ActivityClassificationRule>>> GetCompanyRulesAsync(long companyId, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Failure("Не удалось определить компанию текущего администратора.");
        }

        try
        {
            using var request = CreateAuthorizedRequest(HttpMethod.Get, $"api/admin/companies/{companyId}/classification-rules");
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var rules = await response.Content.ReadFromJsonAsync<List<ActivityClassificationRule>>(_jsonOptions, cancellationToken);
            return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Success(rules ?? []);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<IReadOnlyList<ActivityClassificationRule>>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    public async Task<OperationResult<ActivityClassificationRule>> CreateRuleAsync(ActivityClassificationRuleCreateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = CreateAuthorizedRequest(HttpMethod.Post, "api/admin/classification-rules");
            httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult<ActivityClassificationRule>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var createdRule = await response.Content.ReadFromJsonAsync<ActivityClassificationRule>(_jsonOptions, cancellationToken);
            return createdRule is null
                ? OperationResult<ActivityClassificationRule>.Failure("API вернул пустой ответ после создания правила.", "API_EMPTY_RESPONSE")
                : OperationResult<ActivityClassificationRule>.Success(createdRule);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<ActivityClassificationRule>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<ActivityClassificationRule>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    public async Task<OperationResult<ActivityClassificationRule>> UpdateRuleAsync(long ruleId, ActivityClassificationRuleUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (ruleId <= 0)
        {
            return OperationResult<ActivityClassificationRule>.Failure("Не удалось определить правило для редактирования.");
        }

        try
        {
            using var httpRequest = CreateAuthorizedRequest(HttpMethod.Put, $"api/admin/classification-rules/{ruleId}");
            httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult<ActivityClassificationRule>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var updatedRule = await response.Content.ReadFromJsonAsync<ActivityClassificationRule>(_jsonOptions, cancellationToken);
            return updatedRule is null
                ? OperationResult<ActivityClassificationRule>.Failure("API вернул пустой ответ после обновления правила.", "API_EMPTY_RESPONSE")
                : OperationResult<ActivityClassificationRule>.Success(updatedRule);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<ActivityClassificationRule>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<ActivityClassificationRule>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    public async Task<OperationResult<ActivityClassificationRule>> ToggleRuleAsync(long ruleId, bool isEnabled, CancellationToken cancellationToken = default)
    {
        if (ruleId <= 0)
        {
            return OperationResult<ActivityClassificationRule>.Failure("Не удалось определить правило для изменения статуса.");
        }

        try
        {
            using var httpRequest = CreateAuthorizedRequest(HttpMethod.Post, $"api/admin/classification-rules/{ruleId}/toggle");
            httpRequest.Content = JsonContent.Create(new ActivityClassificationRuleToggleRequest { IsEnabled = isEnabled }, options: _jsonOptions);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult<ActivityClassificationRule>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var updatedRule = await response.Content.ReadFromJsonAsync<ActivityClassificationRule>(_jsonOptions, cancellationToken);
            return updatedRule is null
                ? OperationResult<ActivityClassificationRule>.Failure("API вернул пустой ответ после смены статуса правила.", "API_EMPTY_RESPONSE")
                : OperationResult<ActivityClassificationRule>.Success(updatedRule);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<ActivityClassificationRule>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<ActivityClassificationRule>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    public async Task<OperationResult> DeleteRuleAsync(long ruleId, CancellationToken cancellationToken = default)
    {
        if (ruleId <= 0)
        {
            return OperationResult.Failure("Не удалось определить правило для удаления.");
        }

        try
        {
            using var httpRequest = CreateAuthorizedRequest(HttpMethod.Delete, $"api/admin/classification-rules/{ruleId}");
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            return OperationResult.Success("Правило удалено.");
        }
        catch (HttpRequestException ex)
        {
            return OperationResult.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add(AdminApiRequestHeaders.RequestingUserId, _currentSessionContext.CurrentUser!.Id.ToString());
        return request;
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
