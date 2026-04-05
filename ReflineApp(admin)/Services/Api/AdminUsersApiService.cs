using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Services.Api;

public sealed class AdminUsersApiService : IAdminUsersService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public AdminUsersApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<OperationResult<IReadOnlyList<CompanyUserListItem>>> GetCompanyUsersAsync(long companyId, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            return OperationResult<IReadOnlyList<CompanyUserListItem>>.Failure("Не удалось определить компанию текущего пользователя.");
        }

        try
        {
            using var response = await _httpClient.GetAsync($"api/admin/companies/{companyId}/users", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult<IReadOnlyList<CompanyUserListItem>>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var users = await response.Content.ReadFromJsonAsync<List<CompanyUserListItem>>(_jsonOptions, cancellationToken);
            return OperationResult<IReadOnlyList<CompanyUserListItem>>.Success(users ?? []);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<IReadOnlyList<CompanyUserListItem>>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<IReadOnlyList<CompanyUserListItem>>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
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
