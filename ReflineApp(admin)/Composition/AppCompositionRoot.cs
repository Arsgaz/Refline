using System.Net.Http;
using Refline.Admin.Business.Identity;
using Refline.Admin.Services.Api;
using Refline.Admin.ViewModels;

namespace Refline.Admin.Composition;

public sealed class AppCompositionRoot
{
    public CurrentSessionContext CurrentSessionContext { get; }
    public IAuthenticationService AuthenticationService { get; }
    public IAdminUsersService AdminUsersService { get; }
    public IAdminUserAnalyticsService AdminUserAnalyticsService { get; }
    public ITeamDashboardService TeamDashboardService { get; }
    public IActivityClassificationRulesService ActivityClassificationRulesService { get; }

    public AppCompositionRoot()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:8080/"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        CurrentSessionContext = new CurrentSessionContext();
        AuthenticationService = new AdminAuthenticationService(httpClient, CurrentSessionContext);
        AdminUsersService = new AdminUsersApiService(httpClient, CurrentSessionContext);
        AdminUserAnalyticsService = new AdminUserAnalyticsApiService(httpClient, CurrentSessionContext);
        TeamDashboardService = new TeamDashboardService(AdminUsersService, AdminUserAnalyticsService, CurrentSessionContext);
        ActivityClassificationRulesService = new ActivityClassificationRulesApiService(httpClient, CurrentSessionContext);
    }

    public LoginViewModel CreateLoginViewModel()
    {
        return new LoginViewModel(AuthenticationService);
    }

    public MainViewModel CreateMainViewModel()
    {
        EmployeeAnalyticsViewModel? analyticsViewModel = null;
        TeamDashboardViewModel? teamDashboardViewModel = null;
        ActivityClassificationRulesViewModel? rulesViewModel = null;
        MainViewModel? mainViewModel = null;

        analyticsViewModel = new EmployeeAnalyticsViewModel(
            AdminUserAnalyticsService,
            () => mainViewModel?.ShowEmployeesCommand.Execute(null));

        teamDashboardViewModel = new TeamDashboardViewModel(
            TeamDashboardService,
            CurrentSessionContext);

        rulesViewModel = new ActivityClassificationRulesViewModel(
            ActivityClassificationRulesService,
            CurrentSessionContext);

        var employeesViewModel = new EmployeesViewModel(
            AdminUsersService,
            CurrentSessionContext,
            employee => mainViewModel!.OpenEmployeeAnalyticsAsync(employee));

        mainViewModel = new MainViewModel(
            CurrentSessionContext,
            employeesViewModel,
            analyticsViewModel,
            teamDashboardViewModel,
            new PlaceholderViewModel("Лицензии", "Раздел лицензий пока не реализован."),
            rulesViewModel);

        return mainViewModel;
    }
}
