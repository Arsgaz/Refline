using System.Windows.Input;
using Refline.Admin.Business.Identity;
using Refline.Admin.Utils;

namespace Refline.Admin.ViewModels;

public sealed class ChangePasswordViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly CurrentSessionContext _currentSessionContext;

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public ChangePasswordViewModel(
        IAuthenticationService authenticationService,
        CurrentSessionContext currentSessionContext)
    {
        _authenticationService = authenticationService;
        _currentSessionContext = currentSessionContext;
        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => !IsBusy);
        CancelCommand = new RelayCommand(Cancel, () => !IsBusy);
    }

    public event Action? PasswordChangedSuccessfully;

    public event Action? CancelRequested;

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public string CurrentPassword
    {
        get => _currentPassword;
        set => SetProperty(ref _currentPassword, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
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

    public string UserDisplayName => _currentSessionContext.CurrentUser?.FullName ?? "Администратор";

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = Validate();
        if (!string.IsNullOrWhiteSpace(ErrorMessage))
        {
            return;
        }

        var currentUser = _currentSessionContext.CurrentUser;
        if (currentUser is null)
        {
            ErrorMessage = "Сессия не найдена. Выполните вход заново.";
            return;
        }

        IsBusy = true;

        try
        {
            var result = await _authenticationService.ChangePasswordAsync(
                currentUser.Id,
                CurrentPassword,
                NewPassword);

            if (!result.IsSuccess)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Не удалось сменить пароль."
                    : result.Message;
                return;
            }

            PasswordChangedSuccessfully?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка смены пароля: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string Validate()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            return "Введите текущий пароль.";
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            return "Введите новый пароль.";
        }

        if (string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            return "Подтвердите новый пароль.";
        }

        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            return "Новый пароль и подтверждение не совпадают.";
        }

        if (string.Equals(CurrentPassword, NewPassword, StringComparison.Ordinal))
        {
            return "Новый пароль должен отличаться от текущего.";
        }

        return string.Empty;
    }

    private void Cancel()
    {
        if (IsBusy)
        {
            return;
        }

        CancelRequested?.Invoke();
    }
}
