using System.Windows;
using System.Windows.Input;
using Refline.Admin.Business.Identity;
using Refline.Admin.Models;
using Refline.Admin.Utils;

namespace Refline.Admin.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly CurrentSessionContext _currentSessionContext;
    private object _currentPageViewModel;
    private string _currentSectionTitle;
    private readonly EmployeeAnalyticsViewModel _employeeAnalyticsViewModel;
    private readonly TeamDashboardViewModel _teamDashboardViewModel;

    public MainViewModel(
        CurrentSessionContext currentSessionContext,
        EmployeesViewModel employeesViewModel,
        EmployeeAnalyticsViewModel employeeAnalyticsViewModel,
        TeamDashboardViewModel teamDashboardViewModel,
        PlaceholderViewModel licensesViewModel,
        PlaceholderViewModel rulesViewModel)
    {
        _currentSessionContext = currentSessionContext;
        EmployeesViewModel = employeesViewModel;
        _employeeAnalyticsViewModel = employeeAnalyticsViewModel;
        _teamDashboardViewModel = teamDashboardViewModel;
        LicensesViewModel = licensesViewModel;
        RulesViewModel = rulesViewModel;
        _currentPageViewModel = employeesViewModel;
        _currentSectionTitle = "Сотрудники";

        ShowEmployeesCommand = new RelayCommand(ShowEmployees);
        ShowAnalyticsCommand = new RelayCommand(ShowAnalytics);
        ShowLicensesCommand = new RelayCommand(ShowLicenses);
        ShowRulesCommand = new RelayCommand(ShowRules);
        ExitCommand = new RelayCommand(() => Application.Current.Shutdown());
    }

    public EmployeesViewModel EmployeesViewModel { get; }

    public EmployeeAnalyticsViewModel EmployeeAnalyticsViewModel => _employeeAnalyticsViewModel;

    public TeamDashboardViewModel TeamDashboardViewModel => _teamDashboardViewModel;

    public PlaceholderViewModel LicensesViewModel { get; }

    public PlaceholderViewModel RulesViewModel { get; }

    public ICommand ShowEmployeesCommand { get; }

    public ICommand ShowAnalyticsCommand { get; }

    public ICommand ShowLicensesCommand { get; }

    public ICommand ShowRulesCommand { get; }

    public ICommand ExitCommand { get; }

    public string CurrentUserDisplayName => _currentSessionContext.CurrentUser?.FullName ?? "Неизвестный пользователь";

    public string CurrentUserRoleDisplay => _currentSessionContext.Role switch
    {
        Models.UserRole.Admin => "Администратор",
        Models.UserRole.Manager => "Менеджер",
        _ => "Нет доступа"
    };

    public bool CanViewAnalyticsSection => _currentSessionContext.Role is Models.UserRole.Admin or Models.UserRole.Manager;

    public bool CanViewLicensesSection => _currentSessionContext.Role == Models.UserRole.Admin;

    public bool CanViewRulesSection => _currentSessionContext.Role == Models.UserRole.Admin;

    public string CurrentSectionSubtitle => CurrentSectionTitle switch
    {
        "Сотрудники" => "Пользователи компании, роли и базовый статус аккаунтов.",
        "Карточка сотрудника" => "Сводная аналитика выбранного сотрудника за период.",
        "Аналитика" => "Сводная аналитика по команде или по всей компании.",
        "Лицензии" => "Будущий раздел управления лицензиями компании.",
        "Правила" => "Будущий раздел правил и классификации активности.",
        _ => "Административная консоль компании."
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
        await TeamDashboardViewModel.EnsureLoadedAsync();
    }

    public async Task OpenEmployeeAnalyticsAsync(CompanyUserListItem employee)
    {
        CurrentPageViewModel = EmployeeAnalyticsViewModel;
        CurrentSectionTitle = "Карточка сотрудника";
        OnPropertyChanged(nameof(CurrentSectionSubtitle));
        await EmployeeAnalyticsViewModel.OpenForEmployeeAsync(employee);
    }

    private void ShowEmployees()
    {
        CurrentPageViewModel = EmployeesViewModel;
        CurrentSectionTitle = "Сотрудники";
        OnPropertyChanged(nameof(CurrentSectionSubtitle));
    }

    private void ShowAnalytics()
    {
        CurrentPageViewModel = TeamDashboardViewModel;
        CurrentSectionTitle = "Аналитика";
        OnPropertyChanged(nameof(CurrentSectionSubtitle));
    }

    private void ShowLicenses()
    {
        CurrentPageViewModel = LicensesViewModel;
        CurrentSectionTitle = "Лицензии";
        OnPropertyChanged(nameof(CurrentSectionSubtitle));
    }

    private void ShowRules()
    {
        CurrentPageViewModel = RulesViewModel;
        CurrentSectionTitle = "Правила";
        OnPropertyChanged(nameof(CurrentSectionSubtitle));
    }
}
