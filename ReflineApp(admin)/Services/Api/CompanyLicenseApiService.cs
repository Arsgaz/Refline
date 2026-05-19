using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refline.Admin.Business.Identity;
using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Services.Api;

public sealed class CompanyLicenseApiService : ICompanyLicenseService
{
    private readonly HttpClient _httpClient;
    private readonly Business.Identity.AdminApiAuthorizationService _apiAuthorizationService;
    private readonly CurrentSessionContext _currentSessionContext;
    private readonly JsonSerializerOptions _jsonOptions;

    public CompanyLicenseApiService(
        HttpClient httpClient,
        Business.Identity.AdminApiAuthorizationService apiAuthorizationService,
        CurrentSessionContext currentSessionContext)
    {
        _httpClient = httpClient;
        _apiAuthorizationService = apiAuthorizationService;
        _currentSessionContext = currentSessionContext;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<OperationResult<CompanyLicense?>> GetCompanyLicenseAsync(long companyId, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            return OperationResult<CompanyLicense?>.Failure("Не удалось определить компанию текущего пользователя.");
        }

        if (_currentSessionContext.CurrentUser is null)
        {
            return OperationResult<CompanyLicense?>.Failure("Сессия администратора не найдена.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/admin/companies/{companyId}/license");
            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(request, cancellationToken);
            if (!authorizeResult.IsSuccess)
            {
                return OperationResult<CompanyLicense?>.Failure(authorizeResult.Message, authorizeResult.ErrorCode);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return OperationResult<CompanyLicense?>.Success(null);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult<CompanyLicense?>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var license = await response.Content.ReadFromJsonAsync<CompanyLicense>(_jsonOptions, cancellationToken);
            return license is null
                ? OperationResult<CompanyLicense?>.Failure("API вернул пустой ответ по лицензии.", "API_EMPTY_RESPONSE")
                : OperationResult<CompanyLicense?>.Success(license);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<CompanyLicense?>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<CompanyLicense?>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    public async Task<OperationResult<IReadOnlyList<LicenseDeviceActivation>>> GetLicenseDevicesAsync(CancellationToken cancellationToken = default)
    {
        if (_currentSessionContext.CurrentUser is null)
        {
            return OperationResult<IReadOnlyList<LicenseDeviceActivation>>.Failure("Сессия администратора не найдена.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/admin/licenses/devices");
            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(request, cancellationToken);
            if (!authorizeResult.IsSuccess)
            {
                return OperationResult<IReadOnlyList<LicenseDeviceActivation>>.Failure(authorizeResult.Message, authorizeResult.ErrorCode);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult<IReadOnlyList<LicenseDeviceActivation>>.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            var devices = await response.Content.ReadFromJsonAsync<List<LicenseDeviceActivation>>(_jsonOptions, cancellationToken);
            return OperationResult<IReadOnlyList<LicenseDeviceActivation>>.Success(devices ?? new List<LicenseDeviceActivation>());
        }
        catch (HttpRequestException ex)
        {
            return OperationResult<IReadOnlyList<LicenseDeviceActivation>>.Failure($"API недоступен: {ex.Message}", "API_UNAVAILABLE");
        }
        catch (TaskCanceledException ex)
        {
            return OperationResult<IReadOnlyList<LicenseDeviceActivation>>.Failure($"Превышено время ожидания API: {ex.Message}", "API_TIMEOUT");
        }
    }

    public async Task<OperationResult> RevokeLicenseDeviceAsync(long activationId, CancellationToken cancellationToken = default)
    {
        if (activationId <= 0)
        {
            return OperationResult.Failure("Некорректный идентификатор активации.");
        }

        if (_currentSessionContext.CurrentUser is null)
        {
            return OperationResult.Failure("Сессия администратора не найдена.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/admin/licenses/devices/{activationId}/revoke");
            var authorizeResult = await _apiAuthorizationService.AuthorizeRequestAsync(request, cancellationToken);
            if (!authorizeResult.IsSuccess)
            {
                return OperationResult.Failure(authorizeResult.Message, authorizeResult.ErrorCode);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                return OperationResult.Failure(errorMessage, $"HTTP_{(int)response.StatusCode}");
            }

            return OperationResult.Success();
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
