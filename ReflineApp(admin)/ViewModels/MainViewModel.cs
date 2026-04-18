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
    private readonly LicensesViewModel _licensesViewModel;
    private readonly ActivityClassificationRulesViewModel _rulesViewModel;

    public MainViewModel(
        CurrentSessionContext currentSessionContext,
        EmployeesViewModel employeesViewModel,
        EmployeeAnalyticsViewModel employeeAnalyticsViewModel,
        TeamDashboardViewModel teamDashboardViewModel,
        LicensesViewModel licensesViewModel,
        ActivityClassificationRulesViewModel rulesViewModel)
    {
        _currentSessionContext = currentSessionContext;
        EmployeesViewModel = employeesViewModel;
        _employeeAnalyticsViewModel = employeeAnalyticsViewModel;
        _teamDashboardViewModel = teamDashboardViewModel;
        _licensesViewModel = licensesViewModel;
        _rulesViewModel = rulesViewModel;
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

    public LicensesViewModel LicensesViewModel { get; }

    public ActivityClassificationRulesViewModel RulesViewModel { get; }

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
        "Лицензии" => "Текущая лицензия компании, ключ и состояние активаций устройств.",
        "Правила классификации" => "Company-specific правила классификации активности для приложений и окон.",
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
        await LicensesViewModel.EnsureLoadedAsync();
        await RulesViewModel.EnsureLoadedAsync();
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

    private async void ShowLicenses()
    {
        if (!CanViewLicensesSection)
        {
            return;
        }

        CurrentPageViewModel = LicensesViewModel;
        CurrentSectionTitle = "Лицензии";
        OnPropertyChanged(nameof(CurrentSectionSubtitle));
        await _licensesViewModel.EnsureLoadedAsync();
    }

    private void ShowRules()
    {
        if (!CanViewRulesSection)
        {
            return;
        }

        CurrentPageViewModel = RulesViewModel;
        CurrentSectionTitle = "Правила классификации";
        OnPropertyChanged(nameof(CurrentSectionSubtitle));
        _ = _rulesViewModel.EnsureLoadedAsync();
    }
}
