using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Refline.Models;

public class AppActivity : INotifyPropertyChanged
{
    private int _id;
    private string _appName = string.Empty;
    private string _windowTitle = string.Empty;
    private ActivityCategory _category = ActivityCategory.Unknown;
    private bool _isIdle;
    private bool _isProductive;
    private int _timeSpentSeconds;
    private DateTime _lastActive;
    private DateTime _activityDate = DateTime.Today;
    private int _version;

    public int Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
            }
        }
    }

    public string AppName
    {
        get => _appName;
        set
        {
            if (_appName != value)
            {
                _appName = value;
                OnPropertyChanged();
            }
        }
    }

    public string WindowTitle
    {
        get => _windowTitle;
        set
        {
            if (_windowTitle != value)
            {
                _windowTitle = value;
                OnPropertyChanged();
            }
        }
    }

    public ActivityCategory Category
    {
        get => _category;
        set
        {
            if (_category != value)
            {
                _category = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsIdle
    {
        get => _isIdle;
        set
        {
            if (_isIdle != value)
            {
                _isIdle = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsProductive
    {
        get => _isProductive;
        set
        {
            if (_isProductive != value)
            {
                _isProductive = value;
                OnPropertyChanged();
            }
        }
    }

    public int TimeSpentSeconds
    {
        get => _timeSpentSeconds;
        set
        {
            if (_timeSpentSeconds != value)
            {
                _timeSpentSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DurationString));
            }
        }
    }

    public DateTime LastActive
    {
        get => _lastActive;
        set
        {
            if (_lastActive != value)
            {
                _lastActive = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime ActivityDate
    {
        get => _activityDate;
        set
        {
            if (_activityDate != value)
            {
                _activityDate = value;
                OnPropertyChanged();
            }
        }
    }

    public int Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                OnPropertyChanged();
            }
        }
    }

    public string DurationString
    {
        get
        {
            var ts = TimeSpan.FromSeconds(_timeSpentSeconds);
            if (ts.TotalHours >= 1)
            {
                return $"{(int)ts.TotalHours} ч {ts.Minutes:D2} мин";
            }

            return $"{ts.Minutes} мин {ts.Seconds:D2} сек";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
