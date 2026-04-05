using System.Windows.Input;
using Refline.Business.Activity;
using Refline.Business.Identity;
using Refline.Utils;

namespace Refline.ViewModels;

public class LoginActivationViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILicenseActivationService _licenseActivationService;
    private readonly ICurrentUserSessionStore _currentUserSessionStore;
    private readonly ICompanyActivityClassificationService _companyClassificationService;

    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _licenseKey = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public LoginActivationViewModel(
        IAuthenticationService authenticationService,
        ILicenseActivationService licenseActivationService,
        ICurrentUserSessionStore currentUserSessionStore,
        ICompanyActivityClassificationService companyActivityClassificationService)
    {
        _authenticationService = authenticationService;
        _licenseActivationService = licenseActivationService;
        _currentUserSessionStore = currentUserSessionStore;
        _companyClassificationService = companyActivityClassificationService;
        LoginAndActivateCommand = new RelayCommand(
            async () => await LoginAndActivateAsync(),
            () => !IsBusy);
    }

    public event Action? LoginSucceeded;

    public ICommand LoginAndActivateCommand { get; }
    
    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string LicenseKey
    {
        get => _licenseKey;
        set => SetProperty(ref _licenseKey, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    private async Task LoginAndActivateAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Login) ||
            string.IsNullOrWhiteSpace(Password) ||
            string.IsNullOrWhiteSpace(LicenseKey))
        {
            ErrorMessage = "Заполните логин, пароль и лицензионный ключ.";
            return;
        }

        IsBusy = true;

        try
        {
            var credentialsResult = await _authenticationService.ValidateCredentialsAsync(
                Login.Trim(),
                Password);

            if (!credentialsResult.IsSuccess)
            {
                ErrorMessage = credentialsResult.Message;
                return;
            }

            if (!credentialsResult.Value)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(credentialsResult.Message)
                    ? "Неверный логин или пароль."
                    : credentialsResult.Message;
                return;
            }

            var userResult = await _authenticationService.GetUserByLoginAsync(Login.Trim());
            if (!userResult.IsSuccess)
            {
                ErrorMessage = userResult.Message;
                return;
            }

            if (userResult.Value == null)
            {
                ErrorMessage = "Пользователь не найден.";
                return;
            }

            var activationResult = await _licenseActivationService.ActivateAsync(
                userResult.Value.Id,
                LicenseKey.Trim());

            if (!activationResult.IsSuccess)
            {
                ErrorMessage = activationResult.Message;
                return;
            }

            var currentUser = _currentUserSessionStore.GetCurrentUser();
            if (currentUser != null)
            {
                var restoreRulesResult = await _companyClassificationService.RestoreCachedRulesAsync(currentUser.CompanyId);
                if (!restoreRulesResult.IsSuccess)
                {
                    AppLogger.Log(restoreRulesResult.Message, "ERROR");
                }

                var refreshRulesResult = await _companyClassificationService.RefreshRulesAsync(currentUser.CompanyId);
                if (!refreshRulesResult.IsSuccess)
                {
                    AppLogger.Log($"Company rules refresh after login skipped: {refreshRulesResult.Message}", "ERROR");
                }
            }

            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка входа и активации: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
