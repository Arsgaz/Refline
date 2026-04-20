using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Refline.Admin.Business.Identity;
using Refline.Admin.Models;
using Refline.Admin.Services.Api;
using Refline.Admin.Utils;

namespace Refline.Admin.ViewModels;

public sealed class LicensesViewModel : ViewModelBase
{
    private readonly ICompanyLicenseService _companyLicenseService;
    private readonly CurrentSessionContext _currentSessionContext;
    private CompanyLicense? _license;
    private string _errorMessage = string.Empty;
    private string _copyStatusMessage = string.Empty;
    private bool _isLoading;
    private bool _hasLoaded;
    private readonly DispatcherTimer _autoRefreshTimer;

    public LicensesViewModel(ICompanyLicenseService companyLicenseService, CurrentSessionContext currentSessionContext)
    {
        _companyLicenseService = companyLicenseService;
        _currentSessionContext = currentSessionContext;

        RefreshCommand = new RelayCommand(async () => await LoadAsync(forceReload: true), () => !IsLoading && CanViewLicenses);
        CopyLicenseKeyCommand = new RelayCommand(CopyLicenseKey, () => !IsLoading && HasLicense);

        _autoRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(60)
        };
        _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
        _autoRefreshTimer.Start();
    }

    public ICommand RefreshCommand { get; }

    public ICommand CopyLicenseKeyCommand { get; }

    public CompanyLicense? License
    {
        get => _license;
        private set
        {
            if (SetProperty(ref _license, value))
            {
                RaiseStateChanges();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(IsEmptyStateVisible));
            }
        }
    }

    public string CopyStatusMessage
    {
        get => _copyStatusMessage;
        private set => SetProperty(ref _copyStatusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged(nameof(IsEmptyStateVisible));
            }
        }
    }

    public bool CanViewLicenses => _currentSessionContext.Role == UserRole.Admin;

    public bool HasLicense => License is not null;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmptyStateVisible => !IsLoading && !HasLicense && !HasError;

    public string LicenseTypeDisplay => License?.LicenseType switch
    {
        Models.LicenseType.Basic => "Basic",
        Models.LicenseType.Corporate => "Corporate",
        _ => "Нет данных"
    };

    public string LicenseKindDescription => License?.LicenseType switch
    {
        Models.LicenseType.Basic => "Бессрочная лицензия компании",
        Models.LicenseType.Corporate => "Срочная корпоративная лицензия",
        _ => "Лицензия компании ещё не загружена"
    };

    public string LicenseStatusDisplay
    {
        get
        {
            if (License is null)
            {
                return "Нет данных";
            }

            if (!License.IsActive)
            {
                return "Неактивна";
            }

            if (!License.IsLifetime && License.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return "Истекла";
            }

            return "Активна";
        }
    }

    public string IssuedAtDisplay => License is null
        ? "—"
        : License.IssuedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

    public string ExpiresAtDisplay
    {
        get
        {
            if (License is null)
            {
                return "—";
            }

            if (License.IsLifetime || License.LicenseType == Models.LicenseType.Basic)
            {
                return "Бессрочная";
            }

            return License.ExpiresAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        }
    }

    public string RemainingTimeDisplay
    {
        get
        {
            if (License is null)
            {
                return "—";
            }

            if (License.IsLifetime || License.LicenseType == Models.LicenseType.Basic)
            {
                return "Без ограничения по времени";
            }

            var remaining = License.ExpiresAt - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return "Срок действия истёк";
            }

            if (remaining.TotalDays >= 1)
            {
                return $"Осталось {Math.Floor(remaining.TotalDays):0} дн.";
            }

            if (remaining.TotalHours >= 1)
            {
                return $"Осталось {Math.Floor(remaining.TotalHours):0} ч.";
            }

            return $"Осталось {Math.Max(1, Math.Floor(remaining.TotalMinutes)):0} мин.";
        }
    }

    public string DeviceLimitDisplay
    {
        get
        {
            if (License is null)
            {
                return "—";
            }

            return License.MaxDevices >= int.MaxValue
                ? "Без ограничений"
                : License.MaxDevices.ToString();
        }
    }

    public string ActivatedDevicesDisplay
    {
        get
        {
            if (License is null)
            {
                return "—";
            }

            return License.MaxDevices >= int.MaxValue
                ? $"{License.ActivatedDevicesCount} активировано"
                : $"{License.ActivatedDevicesCount} из {License.MaxDevices}";
        }
    }

    public async Task EnsureLoadedAsync()
    {
        if (_hasLoaded || !CanViewLicenses)
        {
            return;
        }

        await LoadAsync();
    }

    public async Task LoadAsync(bool forceReload = false)
    {
        if (!CanViewLicenses || IsLoading || (_hasLoaded && !forceReload))
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        CopyStatusMessage = string.Empty;

        try
        {
            var result = await _companyLicenseService.GetCompanyLicenseAsync(_currentSessionContext.CompanyId);
            if (!result.IsSuccess)
            {
                License = null;
                _hasLoaded = false;
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Не удалось загрузить лицензию компании."
                    : result.Message;
                return;
            }

            License = result.Value;
            _hasLoaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void AutoRefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (!CanViewLicenses || IsLoading)
        {
            return;
        }

        await LoadAsync(forceReload: true);
    }

    private void CopyLicenseKey()
    {
        if (License is null || string.IsNullOrWhiteSpace(License.LicenseKey))
        {
            return;
        }

        try
        {
            Clipboard.SetText(License.LicenseKey);
            CopyStatusMessage = "Ключ лицензии скопирован.";
        }
        catch (Exception ex)
        {
            CopyStatusMessage = $"Не удалось скопировать ключ: {ex.Message}";
        }
    }

    private void RaiseStateChanges()
    {
        OnPropertyChanged(nameof(HasLicense));
        OnPropertyChanged(nameof(IsEmptyStateVisible));
        OnPropertyChanged(nameof(LicenseTypeDisplay));
        OnPropertyChanged(nameof(LicenseKindDescription));
        OnPropertyChanged(nameof(LicenseStatusDisplay));
        OnPropertyChanged(nameof(IssuedAtDisplay));
        OnPropertyChanged(nameof(ExpiresAtDisplay));
        OnPropertyChanged(nameof(RemainingTimeDisplay));
        OnPropertyChanged(nameof(DeviceLimitDisplay));
        OnPropertyChanged(nameof(ActivatedDevicesDisplay));
        CommandManager.InvalidateRequerySuggested();
    }
}
