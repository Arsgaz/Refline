using System.Net.Http;
using Refline.Business.Activity;
using Refline.Business.Identity;
using Refline.Business.Reports;
using Refline.Business.Settings;
using Refline.Data.Activity;
using Refline.Data.Identity;
using Refline.Data.Infrastructure;
using Refline.Data.Reports;
using Refline.Data.Settings;
using Refline.Services;
using Refline.ViewModels;

namespace Refline.Composition;

public sealed class AppCompositionRoot
{
    public IActivityBusinessServer ActivityBusinessServer { get; }
    public ISettingsBusinessServer SettingsBusinessServer { get; }
    public IReportBusinessServer ReportBusinessServer { get; }
    public IAuthenticationService AuthenticationService { get; }
    public ILicenseActivationService LicenseActivationService { get; }
    public ICurrentUserContext CurrentUserContext { get; }
    public IActivationBootstrapService ActivationBootstrapService { get; }
    public WindowTracker WindowTracker { get; }

    public AppCompositionRoot()
    {
        var apiHttpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:8080"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        var activityDataService = new ActivityDataService();
        var settingsDataService = new SettingsDataService();
        var reportDataService = new ReportDataService();
        var localActivationStateStore = new LocalActivationStateStore();
        var currentUserSessionStateStore = new LocalCurrentUserSessionStateStore();
        var deviceIdentityProvider = new LocalDeviceIdentityProvider();

        CurrentUserContext = new CurrentUserContext();
        var currentUserSessionStore = new CurrentUserSessionStore(currentUserSessionStateStore);

        WindowTracker = new WindowTracker();

        AuthenticationService = new ApiAuthenticationService(
            apiHttpClient,
            CurrentUserContext,
            currentUserSessionStore);
        LicenseActivationService = new ApiLicenseActivationService(
            apiHttpClient,
            localActivationStateStore,
            deviceIdentityProvider,
            CurrentUserContext);

        ActivationBootstrapService = new ActivationBootstrapService(
            localActivationStateStore,
            CurrentUserContext,
            currentUserSessionStore);

        ActivityBusinessServer = new ActivityBusinessServer(
            activityDataService,
            new ActivityValidationService(),
            new ActivityLockService(),
            new ActivityClassificationService(),
            new ActivityMetricsService());

        SettingsBusinessServer = new SettingsBusinessServer(
            settingsDataService,
            new SettingsValidationService(),
            new AutoStartRegistryService());

        ReportBusinessServer = new ReportBusinessServer(
            activityDataService,
            settingsDataService,
            reportDataService,
            new ReportValidationService());
    }

    public MainViewModel CreateMainViewModel()
    {
        return new MainViewModel(ActivityBusinessServer, ReportBusinessServer, WindowTracker);
    }

    public SettingsViewModel CreateSettingsViewModel()
    {
        return new SettingsViewModel(SettingsBusinessServer);
    }

    public LoginActivationViewModel CreateLoginActivationViewModel()
    {
        return new LoginActivationViewModel(AuthenticationService, LicenseActivationService);
    }

    public Task<OperationResult> BootstrapIdentityAsync()
    {
        return BootstrapIdentityInternalAsync();
    }

    private async Task<OperationResult> BootstrapIdentityInternalAsync()
    {
        var bootstrapResult = await ActivationBootstrapService.BootstrapAsync();
        if (!bootstrapResult.IsSuccess)
        {
            return OperationResult.Failure(bootstrapResult.Message, bootstrapResult.ErrorCode);
        }

        return OperationResult.Success(bootstrapResult.Message);
    }
}
