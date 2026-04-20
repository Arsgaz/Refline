using System.Windows;
using Refline.Admin.Composition;
using Refline.Admin.Views;

namespace Refline.Admin;

public partial class App : Application
{
    private AppCompositionRoot? _compositionRoot;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _compositionRoot = new AppCompositionRoot();

        var restoreResult = _compositionRoot.CurrentSessionContext.RestoreAsync().GetAwaiter().GetResult();
        if (restoreResult.IsSuccess && _compositionRoot.CurrentSessionContext.CurrentUser is not null)
        {
            _compositionRoot.ApiAuthorizationService.SetAuthorizationHeader(_compositionRoot.CurrentSessionContext.CurrentSession?.AccessToken);
            if (_compositionRoot.CurrentSessionContext.CurrentUser.MustChangePassword)
            {
                var passwordChanged = ShowChangePasswordWindow();
                if (!passwordChanged)
                {
                    _compositionRoot.AuthenticationService.LogoutAsync().GetAwaiter().GetResult();
                    ShowLoginWindow();
                    return;
                }
            }

            ShowMainWindow();
            return;
        }

        ShowLoginWindow();
    }

    public void ShowMainWindow()
    {
        if (_compositionRoot is null)
        {
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow(_compositionRoot.CreateMainViewModel(LogoutAndReturnToLoginAsync));
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    public void ShowLoginWindow()
    {
        if (_compositionRoot is null)
        {
            Shutdown();
            return;
        }

        var loginWindow = new LoginWindow(_compositionRoot.CreateLoginViewModel());
        var result = loginWindow.ShowDialog();

        if (result == true && _compositionRoot.CurrentSessionContext.CurrentUser is not null)
        {
            _compositionRoot.ApiAuthorizationService.SetAuthorizationHeader(_compositionRoot.CurrentSessionContext.CurrentSession?.AccessToken);
            if (_compositionRoot.CurrentSessionContext.CurrentUser.MustChangePassword)
            {
                var passwordChanged = ShowChangePasswordWindow();
                if (!passwordChanged)
                {
                    _compositionRoot.AuthenticationService.LogoutAsync().GetAwaiter().GetResult();
                    ShowLoginWindow();
                    return;
                }
            }

            ShowMainWindow();
            return;
        }

        Shutdown();
    }

    private bool ShowChangePasswordWindow()
    {
        if (_compositionRoot is null)
        {
            return false;
        }

        var changePasswordWindow = new ChangePasswordWindow(_compositionRoot.CreateChangePasswordViewModel());
        var result = changePasswordWindow.ShowDialog();
        return result == true;
    }

    private async Task LogoutAndReturnToLoginAsync()
    {
        if (_compositionRoot is null)
        {
            Shutdown();
            return;
        }

        var logoutResult = await _compositionRoot.AuthenticationService.LogoutAsync();
        if (!logoutResult.IsSuccess)
        {
            MessageBox.Show(
                logoutResult.Message,
                "Не удалось завершить сессию",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var currentMainWindow = MainWindow;
        MainWindow = null;
        currentMainWindow?.Close();
        ShowLoginWindow();
    }
}
