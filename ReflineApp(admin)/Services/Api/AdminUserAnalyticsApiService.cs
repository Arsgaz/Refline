using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refline.Admin.Business.Identity;
using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Services.Api;

public sealed class AdminUserAnalyticsApiService : IAdminUserAnalyticsService
{
    private readonly HttpClient _httpClient;
    private readonly CurrentSessionContext _currentSessionContext;
    private readonly JsonSerializerOptions _jsonOptions;

    public AdminUserAnalyticsApiService(
        HttpClient httpClient,
        CurrentSessionContext currentSessionContext)
    {
        _httpClient = httpClient;
        _currentSessionContext = currentSessionContext;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<OperationResult<EmployeeAnalyticsSnapshot>> GetEmployeeAnalyticsAsync(
        long userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return OperationResult<EmployeeAnalyticsSnapshot>.Failure("Не удалось определить сотрудника для аналитики.");
        }

        if (_currentSessionContext.CurrentUser is null)
        {
            return OperationResult<EmployeeAnalyticsSnapshot>.Failure("Сессия администратора не найдена.");
        }

        try
        {
            var summaryTask = SendAndReadAsync<UserAnalyticsSummary>(
                $"api/admin/users/{userId}/summary?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
                cancellationToken);
            var breakdownTask = SendAndReadAsync<UserActivityBreakdown>(
                $"api/admin/users/{userId}/activity-breakdown?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
                cancellationToken);

            await Task.WhenAll(summaryTask, breakdownTask);

            var summaryResult = await summaryTask;
            if (!summaryResult.IsSuccess || summaryResult.Value is null)
            {
                return OperationResult<EmployeeAnalyticsSnapshot>.Failure(summaryResult.Message, summaryResult.ErrorCode);
            }

            var breakdownResult = await breakdownTask;
            if (!breakdownResult.IsSuccess || breakdownResult.Value is null)
            {
                return OperationResult<EmployeeAnalyticsSnapshot>.Failure(breakdownResult.Message, breakdownResult.ErrorCode);
            }

            return OperationResult<EmployeeAnalyticsSnapshot>.Success(new EmployeeAnalyticsSnapshot
            {
                Summary = summaryResult.Value,
                Breakdown = breakdownResult.Value
            });
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<EmployeeAnalyticsSnapshot>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<EmployeeAnalyticsSnapshot>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    private async Task<OperationResult<T>> SendAndReadAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        request.Headers.Add(AdminApiRequestHeaders.RequestingUserId, _currentSessionContext.CurrentUser!.Id.ToString());

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            {
                return OperationResult<T>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            return OperationResult<T>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
        if (payload is null)
        {
            return OperationResult<T>.Failure("API вернул пустой ответ.", "API_EMPTY_RESPONSE");
        }

        return OperationResult<T>.Success(payload);
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
