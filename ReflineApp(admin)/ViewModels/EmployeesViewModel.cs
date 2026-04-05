using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Refline.Admin.Business.Identity;
using Refline.Admin.Models;
using Refline.Admin.Services.Api;
using Refline.Admin.Utils;
using Refline.Admin.Views;

namespace Refline.Admin.ViewModels;

public sealed class EmployeesViewModel : ViewModelBase
{
    private readonly IAdminUsersService _adminUsersService;
    private readonly CurrentSessionContext _currentSessionContext;
    private readonly Func<CompanyUserListItem, Task> _openEmployeeAnalyticsAsync;

    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private bool _hasLoaded;
    private CompanyUserListItem? _selectedUser;

    public EmployeesViewModel(
        IAdminUsersService adminUsersService,
        CurrentSessionContext currentSessionContext,
        Func<CompanyUserListItem, Task> openEmployeeAnalyticsAsync)
    {
        _adminUsersService = adminUsersService;
        _currentSessionContext = currentSessionContext;
        _openEmployeeAnalyticsAsync = openEmployeeAnalyticsAsync;
        Users = new ObservableCollection<CompanyUserListItem>();
        RefreshCommand = new RelayCommand(async () => await LoadAsync(forceReload: true), () => !IsLoading);
        CreateUserCommand = new RelayCommand(async () => await CreateUserAsync(), () => !IsLoading && CanManageUsers);
        EditUserCommand = new RelayCommand(
            async parameter => await EditUserAsync(parameter as CompanyUserListItem ?? SelectedUser),
            parameter => !IsLoading && CanManageUsers && (parameter as CompanyUserListItem ?? SelectedUser) is not null);
        ToggleUserActivationCommand = new RelayCommand(
            async parameter => await ToggleUserActivationAsync(parameter as CompanyUserListItem ?? SelectedUser),
            parameter =>
            {
                var user = parameter as CompanyUserListItem ?? SelectedUser;
                return !IsLoading && CanManageUsers && user is not null;
            });
        OpenEmployeeAnalyticsCommand = new RelayCommand(
            async parameter => await OpenEmployeeAnalyticsAsync(parameter as CompanyUserListItem ?? SelectedUser),
            _ => !IsLoading);
    }

    public ObservableCollection<CompanyUserListItem> Users { get; }

    public ICommand RefreshCommand { get; }

    public ICommand CreateUserCommand { get; }

    public ICommand EditUserCommand { get; }

    public ICommand ToggleUserActivationCommand { get; }

    public ICommand OpenEmployeeAnalyticsCommand { get; }

    public CompanyUserListItem? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                CommandManager.InvalidateRequerySuggested();
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

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool HasUsers => Users.Count > 0;

    public bool CanManageUsers => _currentSessionContext.Role == UserRole.Admin;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmptyStateVisible => !IsLoading && !HasUsers && !HasError;

    public string UsersCountText => HasUsers
        ? $"Найдено сотрудников: {Users.Count}"
        : "Сотрудники не загружены";

    public async Task EnsureLoadedAsync()
    {
        if (_hasLoaded)
        {
            return;
        }

        await LoadAsync();
    }

    public async Task LoadAsync(bool forceReload = false)
    {
        if (IsLoading || (_hasLoaded && !forceReload))
        {
            return;
        }

        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var result = await _adminUsersService.GetCompanyUsersAsync(_currentSessionContext.CompanyId);
            if (!result.IsSuccess || result.Value is null)
            {
                Users.Clear();
                _hasLoaded = false;
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Не удалось загрузить список сотрудников."
                    : result.Message;
                OnPropertyChanged(nameof(HasUsers));
                OnPropertyChanged(nameof(UsersCountText));
                return;
            }

            Users.Clear();
            foreach (var user in result.Value)
            {
                Users.Add(user);
            }

            _hasLoaded = true;
            OnPropertyChanged(nameof(HasUsers));
            OnPropertyChanged(nameof(UsersCountText));
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmptyStateVisible));
        }
    }

    private async Task OpenEmployeeAnalyticsAsync(CompanyUserListItem? user)
    {
        if (user is null)
        {
            return;
        }

        await _openEmployeeAnalyticsAsync(user);
    }

    private async Task CreateUserAsync()
    {
        var dialogResult = ShowEditorDialog(null);
        if (dialogResult is null)
        {
            return;
        }

        var normalizedLogin = dialogResult.Login.Trim();
        if (Users.Any(user => string.Equals(user.Login.Trim(), normalizedLogin, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(
                "Пользователь с таким логином уже есть в текущем списке компании.",
                "Логин уже занят",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _adminUsersService.CreateUserAsync(new AdminUserCreateRequest
            {
                FullName = dialogResult.FullName,
                Login = normalizedLogin,
                Password = dialogResult.Password,
                Role = dialogResult.Role,
                ManagerId = dialogResult.ManagerId
            });

            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    string.IsNullOrWhiteSpace(result.Message) ? "Не удалось создать пользователя." : result.Message,
                    "Ошибка создания",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            IsLoading = false;
            await LoadAsync(forceReload: true);
            MessageBox.Show(
                $"Пользователь \"{dialogResult.FullName}\" создан.",
                "Пользователь создан",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task EditUserAsync(CompanyUserListItem? user)
    {
        if (user is null)
        {
            return;
        }

        var dialogResult = ShowEditorDialog(user);
        if (dialogResult is null)
        {
            return;
        }

        var normalizedLogin = dialogResult.Login.Trim();
        if (Users.Any(existingUser =>
                existingUser.Id != user.Id &&
                string.Equals(existingUser.Login.Trim(), normalizedLogin, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(
                "Пользователь с таким логином уже есть в текущем списке компании.",
                "Логин уже занят",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _adminUsersService.UpdateUserAsync(user.Id, new AdminUserUpdateRequest
            {
                FullName = dialogResult.FullName,
                Login = normalizedLogin,
                Role = dialogResult.Role,
                ManagerId = dialogResult.ManagerId
            });

            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    string.IsNullOrWhiteSpace(result.Message) ? "Не удалось обновить пользователя." : result.Message,
                    "Ошибка редактирования",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            IsLoading = false;
            await LoadAsync(forceReload: true);
            MessageBox.Show(
                $"Пользователь \"{dialogResult.FullName}\" обновлен.",
                "Пользователь обновлен",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ToggleUserActivationAsync(CompanyUserListItem? user)
    {
        if (user is null)
        {
            return;
        }

        var isActivation = !user.IsActive;
        var confirmation = MessageBox.Show(
            isActivation
                ? $"Активировать пользователя \"{user.FullName}\"?\n\nПользователь снова получит доступ к системе."
                : $"Деактивировать пользователя \"{user.FullName}\"?\n\nПользователь останется в базе, но потеряет доступ к системе.",
            isActivation ? "Подтверждение активации" : "Подтверждение деактивации",
            MessageBoxButton.YesNo,
            isActivation ? MessageBoxImage.Question : MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = isActivation
                ? await _adminUsersService.ActivateUserAsync(user.Id)
                : await _adminUsersService.DeactivateUserAsync(user.Id);
            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? (isActivation ? "Не удалось активировать пользователя." : "Не удалось деактивировать пользователя.")
                        : result.Message,
                    isActivation ? "Ошибка активации" : "Ошибка деактивации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            IsLoading = false;
            await LoadAsync(forceReload: true);
            MessageBox.Show(
                isActivation
                    ? $"Пользователь \"{user.FullName}\" активирован."
                    : $"Пользователь \"{user.FullName}\" деактивирован.",
                isActivation ? "Пользователь активирован" : "Пользователь деактивирован",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private AdminUserEditorResult? ShowEditorDialog(CompanyUserListItem? user)
    {
        var managerOptions = BuildManagerOptions(user);
        var window = new UserEditorWindow(managerOptions, user)
        {
            Owner = Application.Current.MainWindow
        };

        return window.ShowDialog() == true
            ? window.Result
            : null;
    }

    private IReadOnlyList<ManagerOption> BuildManagerOptions(CompanyUserListItem? editedUser)
    {
        var managerOptions = Users
            .Where(user =>
                user.IsActive &&
                user.Role is UserRole.Admin or UserRole.Manager &&
                (editedUser is null || user.Id != editedUser.Id))
            .OrderBy(user => user.Role)
            .ThenBy(user => user.FullName)
            .Select(user => new ManagerOption
            {
                Id = user.Id,
                DisplayName = $"{user.FullName} ({user.RoleDisplay})"
            })
            .ToList();

        managerOptions.Insert(0, new ManagerOption
        {
            Id = null,
            DisplayName = "Без менеджера"
        });

        return managerOptions;
    }
}
