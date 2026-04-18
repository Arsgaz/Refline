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
    public ICompanyLicenseService CompanyLicenseService { get; }

    public AppCompositionRoot()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:8080/"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        var sessionStateStore = new LocalCurrentSessionStateStore();
        CurrentSessionContext = new CurrentSessionContext(sessionStateStore);
        var apiAuthorizationService = new AdminApiAuthorizationService(httpClient, CurrentSessionContext);
        AuthenticationService = new AdminAuthenticationService(httpClient, apiAuthorizationService, CurrentSessionContext);
        AdminUsersService = new AdminUsersApiService(httpClient, apiAuthorizationService, CurrentSessionContext);
        AdminUserAnalyticsService = new AdminUserAnalyticsApiService(httpClient, apiAuthorizationService, CurrentSessionContext);
        TeamDashboardService = new TeamDashboardService(AdminUsersService, AdminUserAnalyticsService, CurrentSessionContext);
        ActivityClassificationRulesService = new ActivityClassificationRulesApiService(httpClient, apiAuthorizationService, CurrentSessionContext);
        CompanyLicenseService = new CompanyLicenseApiService(httpClient, apiAuthorizationService, CurrentSessionContext);
    }

    public LoginViewModel CreateLoginViewModel()
    {
        return new LoginViewModel(AuthenticationService);
    }

    public ChangePasswordViewModel CreateChangePasswordViewModel()
    {
        return new ChangePasswordViewModel(AuthenticationService, CurrentSessionContext);
    }

    public MainViewModel CreateMainViewModel()
    {
        EmployeeAnalyticsViewModel? analyticsViewModel = null;
        TeamDashboardViewModel? teamDashboardViewModel = null;
        ActivityClassificationRulesViewModel? rulesViewModel = null;
        LicensesViewModel? licensesViewModel = null;
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

        licensesViewModel = new LicensesViewModel(
            CompanyLicenseService,
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
            licensesViewModel,
            rulesViewModel);

        return mainViewModel;
    }
}
