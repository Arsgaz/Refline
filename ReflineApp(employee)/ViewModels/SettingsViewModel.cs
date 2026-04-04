using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Refline.Business.Identity;
using Refline.Business.Settings;
using Refline.Data.Identity;
using Refline.Models;
using Refline.Utils;

namespace Refline.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsBusinessServer _settingsBusinessServer;
    private readonly ICurrentUserSessionStore _currentUserSessionStore;
    private readonly ILocalActivationStateStore _localActivationStateStore;
    private readonly ICurrentUserContext _currentUserContext;

    private string _reportsPath = string.Empty;
    private bool _autoStartWindows;
    private bool _allowBackgroundTracking = true;
    private bool _enableLocalLog = true;

    public SettingsViewModel(
        ISettingsBusinessServer settingsBusinessServer,
        ICurrentUserSessionStore currentUserSessionStore,
        ILocalActivationStateStore localActivationStateStore,
        ICurrentUserContext currentUserContext)
    {
        _settingsBusinessServer = settingsBusinessServer;
        _currentUserSessionStore = currentUserSessionStore;
        _localActivationStateStore = localActivationStateStore;
        _currentUserContext = currentUserContext;

        SelectFolderCommand = new RelayCommand(ExecuteSelectFolder);
        SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings);
        LogoutCommand = new RelayCommand(async () => await ExecuteLogoutAsync());

        LoadSettings();
    }

    public event Action? LogoutCompleted;

    public string ReportsPath
    {
        get => _reportsPath;
        set => SetProperty(ref _reportsPath, value);
    }

    public bool AutoStartWindows
    {
        get => _autoStartWindows;
        set => SetProperty(ref _autoStartWindows, value);
    }

    public bool AllowBackgroundTracking
    {
        get => _allowBackgroundTracking;
        set => SetProperty(ref _allowBackgroundTracking, value);
    }

    public bool EnableLocalLog
    {
        get => _enableLocalLog;
        set => SetProperty(ref _enableLocalLog, value);
    }

    public ICommand SelectFolderCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand LogoutCommand { get; }

    private void LoadSettings()
    {
        var loadResult = _settingsBusinessServer.LoadSettings();
        if (!loadResult.IsSuccess || loadResult.Value == null)
        {
            MessageBox.Show(loadResult.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ReportsPath = loadResult.Value.ReportsPath;
        AutoStartWindows = loadResult.Value.AutoStartWindows;
        AllowBackgroundTracking = loadResult.Value.AllowBackgroundTracking;
        EnableLocalLog = loadResult.Value.EnableLocalLog;
    }

    private void ExecuteSelectFolder()
    {
        var folderBrowser = new OpenFolderDialog
        {
            Title = "Выберите папку для сохранения отчётов",
            InitialDirectory = string.IsNullOrWhiteSpace(ReportsPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : ReportsPath
        };

        if (folderBrowser.ShowDialog() == true)
        {
            ReportsPath = folderBrowser.FolderName;
        }
    }

    private void ExecuteSaveSettings()
    {
        var settings = new AppSettings
        {
            ReportsPath = ReportsPath,
            AutoStartWindows = AutoStartWindows,
            AllowBackgroundTracking = AllowBackgroundTracking,
            EnableLocalLog = EnableLocalLog
        };

        var saveResult = _settingsBusinessServer.SaveSettings(settings);
        if (!saveResult.IsSuccess)
        {
            MessageBox.Show(saveResult.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AppLogger.Log("Settings saved.");
        MessageBox.Show("Настройки успешно сохранены!", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task ExecuteLogoutAsync()
    {
        var clearSessionResult = await _currentUserSessionStore.ClearAsync();
        if (!clearSessionResult.IsSuccess)
        {
            MessageBox.Show(clearSessionResult.Message, "Ошибка выхода", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var clearActivationResult = await _localActivationStateStore.ClearAsync();
        if (!clearActivationResult.IsSuccess)
        {
            MessageBox.Show(clearActivationResult.Message, "Ошибка выхода", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _currentUserContext.Clear();
        AppLogger.Log("User logged out.");
        LogoutCompleted?.Invoke();
    }
}
