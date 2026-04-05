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

        ShowLoginWindow();
    }

    public void ShowMainWindow()
    {
        if (_compositionRoot is null)
        {
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow(_compositionRoot.CreateMainViewModel());
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
            ShowMainWindow();
            return;
        }

        Shutdown();
    }
}
