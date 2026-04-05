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

    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private bool _hasLoaded;

    public EmployeesViewModel(IAdminUsersService adminUsersService, CurrentSessionContext currentSessionContext)
    {
        _adminUsersService = adminUsersService;
        _currentSessionContext = currentSessionContext;
        Users = new ObservableCollection<CompanyUserListItem>();
        RefreshCommand = new RelayCommand(async () => await LoadAsync(forceReload: true), () => !IsLoading);
    }

    public ObservableCollection<CompanyUserListItem> Users { get; }

    public ICommand RefreshCommand { get; }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
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
                return;
            }

            Users.Clear();
            foreach (var user in result.Value)
            {
                Users.Add(user);
            }

            _hasLoaded = true;
            OnPropertyChanged(nameof(HasUsers));
        }
        finally
        {
            IsLoading = false;
        }
    }
}
