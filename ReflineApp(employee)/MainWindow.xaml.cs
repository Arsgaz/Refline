using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
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

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Включаем эффект Mica только для Windows 11 (build 22000+)
        if (Environment.OSVersion.Version.Build >= 22000)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            int trueValue = 1;
            DwmSetWindowAttribute(hwnd, 20, ref trueValue, sizeof(int)); // DWMWA_USE_IMMERSIVE_DARK_MODE

            int acrylicValue = 3; // DWMSBT_TRANSIENTWINDOW (Acrylic)
            DwmSetWindowAttribute(hwnd, 38, ref acrylicValue, sizeof(int)); // DWMWA_SYSTEMBACKDROP_TYPE

            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);

            // Решение проблемы "черного экрана WPF" без использования багованного WindowChrome:
            // Очищаем фоновый цвет движка рендеринга WPF
            if (HwndSource.FromHwnd(hwnd) is HwndSource source && source.CompositionTarget != null)
            {
                source.CompositionTarget.BackgroundColor = Colors.Transparent;
            }

            // Делаем фон окна прозрачным, чтобы пропустить эффект DWM
            Background = Brushes.Transparent;
        }
        else
        {
            // Fallback для Windows 10
            Background = new SolidColorBrush(Color.FromRgb(13, 17, 23)); // #0D1117
        }
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
        // Принудительно обновляем данные после перехода, чтобы LiveCharts получил данные
        // уже ПОСЛЕ того как получил реальный размер на экране (Layout pass)
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            _mainViewModel.RefreshReportData();
        });
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
