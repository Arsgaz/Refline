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
        var activityDataService = new ActivityDataService();
        var settingsDataService = new SettingsDataService();
        var reportDataService = new ReportDataService();
        var userStore = new LocalUserStore();
        var licenseStore = new LocalLicenseStore();
        var deviceActivationStore = new LocalDeviceActivationStore();
        var localActivationStateStore = new LocalActivationStateStore();
        var deviceIdentityProvider = new LocalDeviceIdentityProvider();

        CurrentUserContext = new CurrentUserContext();

        WindowTracker = new WindowTracker();

        AuthenticationService = new LocalAuthenticationService(userStore, CurrentUserContext);
        LicenseActivationService = new LocalLicenseActivationService(
            userStore,
            licenseStore,
            deviceActivationStore,
            localActivationStateStore,
            deviceIdentityProvider,
            CurrentUserContext);

        ActivationBootstrapService = new ActivationBootstrapService(
            localActivationStateStore,
            userStore,
            CurrentUserContext);

        ActivityBusinessServer = new ActivityBusinessServer(
            activityDataService,
            new ActivityValidationService(),
            new ActivityLockService());

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
