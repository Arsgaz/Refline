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
    }

    public LoginViewModel CreateLoginViewModel()
    {
        return new LoginViewModel(AuthenticationService);
    }

    public MainViewModel CreateMainViewModel()
    {
        var employeesViewModel = new EmployeesViewModel(AdminUsersService, CurrentSessionContext);

        return new MainViewModel(
            CurrentSessionContext,
            employeesViewModel,
            new PlaceholderViewModel("Аналитика", "Раздел аналитики будет добавлен следующим этапом."),
            new PlaceholderViewModel("Лицензии", "Раздел лицензий пока не реализован."),
            new PlaceholderViewModel("Правила", "Раздел правил и классификаций будет добавлен позже."));
    }
}
