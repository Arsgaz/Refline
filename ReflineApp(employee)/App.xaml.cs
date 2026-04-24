using System.Windows;
using Refline.Composition;
using Refline.Utils;
using Refline.Views;

namespace Refline;

public partial class App : Application
{
    private AppCompositionRoot? _composition;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _composition = new AppCompositionRoot();
        var bootstrapResult = _composition.BootstrapIdentityAsync().GetAwaiter().GetResult();
        if (!bootstrapResult.IsSuccess)
        {
            AppLogger.Log($"Identity bootstrap warning: {bootstrapResult.Message}");
        }

        if (ShouldOpenMainWindow() && CanProceedAfterPasswordChange())
        {
            OpenMainWindow();
            AppLogger.Log("Application started.");
            return;
        }

        var loginViewModel = _composition.CreateLoginActivationViewModel();
        var loginWindow = new LoginActivationWindow(loginViewModel);
        var loginResult = loginWindow.ShowDialog();

        if (loginResult == true && ShouldOpenMainWindow() && CanProceedAfterPasswordChange())
        {
            OpenMainWindow();
            AppLogger.Log("Application started after login activation.");
            return;
        }

        AppLogger.Log("Application startup cancelled: login activation was not completed.");
        Shutdown();
    }

    private bool ShouldOpenMainWindow()
    {
        if (_composition == null)
        {
            return false;
        }

        var activationResult = _composition.LicenseActivationService.IsActivatedAsync().GetAwaiter().GetResult();
        if (!activationResult.IsSuccess || !activationResult.Value)
        {
            return false;
        }

        var validationResult = _composition.LicenseActivationService.ValidateCurrentActivationAsync().GetAwaiter().GetResult();
        if (validationResult.IsSuccess && validationResult.Value != null)
        {
            if (validationResult.Value.Status == Business.Identity.CurrentActivationValidationStatus.Revoked)
            {
                MessageBox.Show(
                    "Это устройство было отвязано от лицензии. Выполните активацию заново.",
                    "Активация отозвана",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (validationResult.Value.Status == Business.Identity.CurrentActivationValidationStatus.NotActivated)
            {
                return false;
            }
        }

        var currentUserResult = _composition.AuthenticationService.GetCurrentUserAsync().GetAwaiter().GetResult();
        return currentUserResult.IsSuccess && currentUserResult.Value != null;
    }

    private bool CanProceedAfterPasswordChange()
    {
        if (_composition == null)
        {
            return false;
        }

        var currentUserResult = _composition.AuthenticationService.GetCurrentUserAsync().GetAwaiter().GetResult();
        if (!currentUserResult.IsSuccess || currentUserResult.Value == null)
        {
            return false;
        }

        if (!currentUserResult.Value.MustChangePassword)
        {
            return true;
        }

        var changePasswordWindow = new ChangePasswordWindow(_composition.CreateChangePasswordViewModel());
        return changePasswordWindow.ShowDialog() == true;
    }

    private void OpenMainWindow()
    {
        if (_composition == null)
        {
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow(
            _composition.CreateMainViewModel(),
            _composition.CreateSettingsViewModel(),
            _composition.SettingsBusinessServer);

        MainWindow = mainWindow;
        mainWindow.Show();
    }

    public void ShowLoginWindowAfterLogout(Window currentWindow)
    {
        if (_composition == null)
        {
            Shutdown();
            return;
        }

        currentWindow.Hide();
        MainWindow = null;

        var loginViewModel = _composition.CreateLoginActivationViewModel();
        var loginWindow = new LoginActivationWindow(loginViewModel);
        var loginResult = loginWindow.ShowDialog();

        currentWindow.Close();

        if (loginResult == true && ShouldOpenMainWindow() && CanProceedAfterPasswordChange())
        {
            OpenMainWindow();
            AppLogger.Log("Application restarted after logout/login.");
            return;
        }

        AppLogger.Log("Application shutdown after logout: login activation was not completed.");
        Shutdown();
    }
}
