using System.Collections.ObjectModel;
using System.Windows.Input;
using Refline.Admin.Business.Identity;
using Refline.Admin.Models;
using Refline.Admin.Services.Api;
using Refline.Admin.Utils;

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
        OpenEmployeeAnalyticsCommand = new RelayCommand(
            async parameter => await OpenEmployeeAnalyticsAsync(parameter as CompanyUserListItem ?? SelectedUser),
            _ => !IsLoading);
    }

    public ObservableCollection<CompanyUserListItem> Users { get; }

    public ICommand RefreshCommand { get; }

    public ICommand OpenEmployeeAnalyticsCommand { get; }

    public CompanyUserListItem? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
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
}
