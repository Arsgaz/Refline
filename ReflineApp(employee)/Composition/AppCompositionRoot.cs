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
using Refline.Services.ActivityClassification;
using Refline.Services.ActivitySync;
using Refline.ViewModels;

namespace Refline.Composition;

public sealed class AppCompositionRoot
{
    private readonly ILocalActivationStateStore _localActivationStateStore;
    private readonly ICurrentUserSessionStore _currentUserSessionStore;

    public IActivityBusinessServer ActivityBusinessServer { get; }
    public ISettingsBusinessServer SettingsBusinessServer { get; }
    public IReportBusinessServer ReportBusinessServer { get; }
    public IAuthenticationService AuthenticationService { get; }
    public ILicenseActivationService LicenseActivationService { get; }
    public ICurrentUserContext CurrentUserContext { get; }
    public IActivationBootstrapService ActivationBootstrapService { get; }
    public WindowTracker WindowTracker { get; }
    public IActivitySyncService ActivitySyncService { get; }
    public ICompanyActivityClassificationService CompanyActivityClassificationService { get; }

    public AppCompositionRoot()
    {
        var apiHttpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:8080"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        var activityDataService = new ActivityDataService();
        var pendingActivityStore = new LocalPendingActivityStore();
        var activityClassificationRuleStore = new LocalActivityClassificationRuleStore();
        var settingsDataService = new SettingsDataService();
        var reportDataService = new ReportDataService();
        _localActivationStateStore = new LocalActivationStateStore();
        var currentUserSessionStateStore = new LocalCurrentUserSessionStateStore();
        var deviceIdentityProvider = new LocalDeviceIdentityProvider();

        CurrentUserContext = new CurrentUserContext();
        _currentUserSessionStore = new CurrentUserSessionStore(currentUserSessionStateStore);
        CompanyActivityClassificationService = new CompanyActivityClassificationService(
            activityClassificationRuleStore,
            new ActivityClassificationRulesApiService(apiHttpClient, _currentUserSessionStore));

        WindowTracker = new WindowTracker();
        ActivitySyncService = new ApiActivitySyncService(
            apiHttpClient,
            pendingActivityStore,
            _currentUserSessionStore,
            CompanyActivityClassificationService);

        AuthenticationService = new ApiAuthenticationService(
            apiHttpClient,
            CurrentUserContext,
            _currentUserSessionStore);
        LicenseActivationService = new ApiLicenseActivationService(
            apiHttpClient,
            _localActivationStateStore,
            deviceIdentityProvider,
            CurrentUserContext);

        ActivationBootstrapService = new ActivationBootstrapService(
            _localActivationStateStore,
            CurrentUserContext,
            _currentUserSessionStore);

        ActivityBusinessServer = new ActivityBusinessServer(
            activityDataService,
            new ActivityValidationService(),
            new ActivityLockService(),
            new CompositeActivityClassificationService(
                CompanyActivityClassificationService,
                new ActivityClassificationService()),
            new ActivityMetricsService(),
            pendingActivityStore,
            CurrentUserContext,
            _localActivationStateStore);

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
        return new MainViewModel(ActivityBusinessServer, ReportBusinessServer, ActivitySyncService, WindowTracker);
    }

    public SettingsViewModel CreateSettingsViewModel()
    {
        return new SettingsViewModel(
            SettingsBusinessServer,
            _currentUserSessionStore,
            _localActivationStateStore,
            CurrentUserContext);
    }

    public LoginActivationViewModel CreateLoginActivationViewModel()
    {
        return new LoginActivationViewModel(
            AuthenticationService,
            LicenseActivationService,
            _currentUserSessionStore,
            CompanyActivityClassificationService);
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

        var currentUser = _currentUserSessionStore.GetCurrentUser();
        if (currentUser != null)
        {
            var restoreRulesResult = await CompanyActivityClassificationService.RestoreCachedRulesAsync(currentUser.CompanyId);
            if (!restoreRulesResult.IsSuccess)
            {
                Refline.Utils.AppLogger.Log(restoreRulesResult.Message, "ERROR");
            }
        }

        return OperationResult.Success(bootstrapResult.Message);
    }
}
