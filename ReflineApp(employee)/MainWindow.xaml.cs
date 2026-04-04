using System.ComponentModel;
using System.Windows;
using Refline.Business.Settings;
using Refline.ViewModels;
using Refline.Views;

namespace Refline;

public partial class MainWindow : Window
{
    private readonly MainViewModel _mainViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly ISettingsBusinessServer _settingsBusinessServer;

    private DashboardPage? _dashboardPage;
    private ReportsPage? _reportsPage;
    private SettingsPage? _settingsPage;
    private bool _isLogoutInProgress;

    public MainWindow(
        MainViewModel mainViewModel,
        SettingsViewModel settingsViewModel,
        ISettingsBusinessServer settingsBusinessServer)
    {
        InitializeComponent();

        _mainViewModel = mainViewModel;
        _settingsViewModel = settingsViewModel;
        _settingsBusinessServer = settingsBusinessServer;

        DataContext = _mainViewModel;

        _dashboardPage = new DashboardPage { DataContext = _mainViewModel };
        MainFrame.Navigate(_dashboardPage);

        _settingsViewModel.LogoutCompleted += OnLogoutCompleted;
        Closing += MainWindow_Closing;
    }

    private void DashboardBtn_Click(object sender, RoutedEventArgs e)
    {
        _dashboardPage ??= new DashboardPage { DataContext = _mainViewModel };
        MainFrame.Navigate(_dashboardPage);
    }

    private void ReportsBtn_Click(object sender, RoutedEventArgs e)
    {
        _reportsPage ??= new ReportsPage { DataContext = _mainViewModel };
        MainFrame.Navigate(_reportsPage);
    }

    private void SettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        _settingsPage ??= new SettingsPage { DataContext = _settingsViewModel };
        MainFrame.Navigate(_settingsPage);
    }

    private void ExitBtn_Click(object sender, RoutedEventArgs e)
    {
        _mainViewModel.OnClosing();
        Application.Current.Shutdown();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_isLogoutInProgress)
        {
            _settingsViewModel.LogoutCompleted -= OnLogoutCompleted;
            _mainViewModel.OnClosing();
            return;
        }

        var allowBackgroundResult = _settingsBusinessServer.IsBackgroundTrackingAllowed();
        if (allowBackgroundResult.IsSuccess && allowBackgroundResult.Value)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _mainViewModel.OnClosing();
        Application.Current.Shutdown();
    }

    private void AppNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
    }

    private void TrayRestore_Click(object sender, RoutedEventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
    }

    private void TrayExit_Click(object sender, RoutedEventArgs e)
    {
        _mainViewModel.OnClosing();
        Application.Current.Shutdown();
    }

    private void OnLogoutCompleted()
    {
        _isLogoutInProgress = true;
        ((App)Application.Current).ShowLoginWindowAfterLogout(this);
    }
}
