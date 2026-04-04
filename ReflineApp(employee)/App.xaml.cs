using System.Windows;
using Refline.Composition;
using Refline.Utils;

namespace Refline;

public partial class App : Application
{
    private AppCompositionRoot? _composition;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _composition = new AppCompositionRoot();
        var bootstrapResult = _composition.BootstrapIdentityAsync().GetAwaiter().GetResult();
        if (!bootstrapResult.IsSuccess)
        {
            AppLogger.Log($"Identity bootstrap warning: {bootstrapResult.Message}");
        }

        var mainWindow = new MainWindow(
            _composition.CreateMainViewModel(),
            _composition.CreateSettingsViewModel(),
            _composition.SettingsBusinessServer);

        MainWindow = mainWindow;
        mainWindow.Show();

        AppLogger.Log("Application started.");
    }
}
