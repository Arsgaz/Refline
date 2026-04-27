using System.Net.Http;
using Refline.Admin.Business.Identity;
using Refline.Admin.Services.Api;
using Refline.Admin.ViewModels;

namespace Refline.Admin.Composition;

public sealed class AppCompositionRoot
{
    public CurrentSessionContext CurrentSessionContext { get; }
    public IAuthenticationService AuthenticationService { get; }
    public AdminApiAuthorizationService ApiAuthorizationService { get; }
    public IAdminUsersService AdminUsersService { get; }
    public IAdminUserAnalyticsService AdminUserAnalyticsService { get; }
    public ITeamDashboardService TeamDashboardService { get; }
    public IActivityClassificationRulesService ActivityClassificationRulesService { get; }
    public ICompanyLicenseService CompanyLicenseService { get; }

    public AppCompositionRoot()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://refline.local:8080/"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        var sessionStateStore = new LocalCurrentSessionStateStore();
        CurrentSessionContext = new CurrentSessionContext(sessionStateStore);
        ApiAuthorizationService = new AdminApiAuthorizationService(httpClient, CurrentSessionContext);
        AuthenticationService = new AdminAuthenticationService(httpClient, ApiAuthorizationService, CurrentSessionContext);
        AdminUsersService = new AdminUsersApiService(httpClient, ApiAuthorizationService, CurrentSessionContext);
        AdminUserAnalyticsService = new AdminUserAnalyticsApiService(httpClient, ApiAuthorizationService, CurrentSessionContext);
        TeamDashboardService = new TeamDashboardService(AdminUsersService, AdminUserAnalyticsService, CurrentSessionContext);
        ActivityClassificationRulesService = new ActivityClassificationRulesApiService(httpClient, ApiAuthorizationService, CurrentSessionContext);
        CompanyLicenseService = new CompanyLicenseApiService(httpClient, ApiAuthorizationService, CurrentSessionContext);
    }

    public LoginViewModel CreateLoginViewModel()
    {
        return new LoginViewModel(AuthenticationService);
    }

    public ChangePasswordViewModel CreateChangePasswordViewModel()
    {
        return new ChangePasswordViewModel(AuthenticationService, CurrentSessionContext);
    }

    public MainViewModel CreateMainViewModel(Func<Task> logoutAndReturnToLoginAsync)
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
            logoutAndReturnToLoginAsync,
            employeesViewModel,
            analyticsViewModel,
            teamDashboardViewModel,
            licensesViewModel,
            rulesViewModel);

        return mainViewModel;
    }
}
