using System.Windows.Input;
using Refline.Admin.Business.Identity;
using Refline.Admin.Utils;

namespace Refline.Admin.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;

    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public LoginViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        LoginCommand = new RelayCommand(async () => await LoginAsync(), () => !IsBusy);
    }

    public event Action? LoginSucceeded;

    public ICommand LoginCommand { get; }

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

    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = string.Empty;
        IsBusy = true;

        try
        {
            var result = await _authenticationService.LoginAsync(Login.Trim(), Password);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Не удалось выполнить вход."
                    : result.Message;
                return;
            }

            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка входа: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
