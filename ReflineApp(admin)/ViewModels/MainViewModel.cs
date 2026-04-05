using System.Windows;
using System.Windows.Input;
using Refline.Admin.Business.Identity;
using Refline.Admin.Utils;

namespace Refline.Admin.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly CurrentSessionContext _currentSessionContext;
    private object _currentPageViewModel;
    private string _currentSectionTitle;

    public MainViewModel(
        CurrentSessionContext currentSessionContext,
        EmployeesViewModel employeesViewModel,
        PlaceholderViewModel analyticsViewModel,
        PlaceholderViewModel licensesViewModel)
    {
        _currentSessionContext = currentSessionContext;
        EmployeesViewModel = employeesViewModel;
        AnalyticsViewModel = analyticsViewModel;
        LicensesViewModel = licensesViewModel;
        _currentPageViewModel = employeesViewModel;
        _currentSectionTitle = "Сотрудники";

        ShowEmployeesCommand = new RelayCommand(ShowEmployees);
        ShowAnalyticsCommand = new RelayCommand(ShowAnalytics);
        ShowLicensesCommand = new RelayCommand(ShowLicenses);
        ExitCommand = new RelayCommand(() => Application.Current.Shutdown());
    }

    public EmployeesViewModel EmployeesViewModel { get; }

    public PlaceholderViewModel AnalyticsViewModel { get; }

    public PlaceholderViewModel LicensesViewModel { get; }

    public ICommand ShowEmployeesCommand { get; }

    public ICommand ShowAnalyticsCommand { get; }

    public ICommand ShowLicensesCommand { get; }

    public ICommand ExitCommand { get; }

    public string CurrentUserDisplayName => _currentSessionContext.CurrentUser?.FullName ?? "Неизвестный пользователь";

    public string CurrentUserRoleDisplay => _currentSessionContext.Role switch
    {
        Models.UserRole.Admin => "Администратор",
        Models.UserRole.Manager => "Менеджер",
        _ => "Нет доступа"
    };

    public object CurrentPageViewModel
    {
        get => _currentPageViewModel;
        private set => SetProperty(ref _currentPageViewModel, value);
    }

    public string CurrentSectionTitle
    {
        get => _currentSectionTitle;
        private set => SetProperty(ref _currentSectionTitle, value);
    }

    public async Task InitializeAsync()
    {
        await EmployeesViewModel.EnsureLoadedAsync();
    }

    private void ShowEmployees()
    {
        CurrentPageViewModel = EmployeesViewModel;
        CurrentSectionTitle = "Сотрудники";
    }

    private void ShowAnalytics()
    {
        CurrentPageViewModel = AnalyticsViewModel;
        CurrentSectionTitle = "Аналитика";
    }

    private void ShowLicenses()
    {
        CurrentPageViewModel = LicensesViewModel;
        CurrentSectionTitle = "Лицензии";
    }
}
